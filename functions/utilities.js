const functions = require('firebase-functions');
const admin = require('firebase-admin');
const notifications = require('./notifications');

/**
 * Cleans up old notifications (older than 30 days)
 */
async function cleanupOldNotifications(db)
{
    try
    {
        const thirtyDaysAgo = new Date();
        thirtyDaysAgo.setDate(thirtyDaysAgo.getDate() - 30);

        const oldNotifications = await db.collection('notifications')
          .where('createdAt', '<', thirtyDaysAgo)
          .where('isRead', '==', true)
          .limit(500)
          .get();

        if (oldNotifications.empty)
        {
            console.log('No old notifications to clean up');
            return null;
        }

        const batch = db.batch();
        let deleteCount = 0;

        oldNotifications.forEach(doc => {
            batch.delete(doc.ref);
            deleteCount++;
        });

        await batch.commit();
        console.log(`Deleted ${ deleteCount}
        old notifications`);

        return { deleted: deleteCount }
        ;
    }
    catch (error)
    {
        console.error('Error cleaning up notifications:', error);
        throw error;
    }
}

/**
 * Auto-denies pending visits that are past their scheduled date
 */
async function autoExpirePendingVisits(db, messaging)
{
    try
    {
        const now = new Date();
        now.setHours(0, 0, 0, 0);

        const expiredVisits = await db.collection('visits')
          .where('status', '==', 'pending')
          .where('visitDate', '<', now)
          .get();

        if (expiredVisits.empty)
        {
            console.log('No expired pending visits');
            return null;
        }

        const batch = db.batch();
        const expiredIds = [];

        for (const doc of expiredVisits.docs) {
            const visit = doc.data();

            batch.update(doc.ref, {
            status: 'denied',
        denialReason: 'Visit request expired - scheduled date has passed',
        approvedBy: 'system',
        approvedAt: admin.firestore.FieldValue.serverTimestamp(),
        updatedAt: admin.firestore.FieldValue.serverTimestamp()
            });

            expiredIds.push(doc.id);

            // Send expiration notification
            await notifications.sendVisitDeniedNotification(
              doc.id,
              'Visit request expired - scheduled date has passed',
              db,
              messaging
            );
        }

        await batch.commit();
        console.log(`Auto - denied ${ expiredIds.length}
        expired visits`);

        return { expired: expiredIds.length, visitIds: expiredIds }
        ;
    }
    catch (error)
    {
        console.error('Error auto-expiring visits:', error);
        throw error;
    }
}

/**
 * Generates QR code data with security signature
 */
async function generateQRCode(data, context)
{
    if (!context.auth)
    {
        throw new functions.https.HttpsError('unauthenticated', 'User must be authenticated');
    }

    const { visitId } = data;

    if (!visitId)
    {
        throw new functions.https.HttpsError('invalid-argument', 'visitId is required');
    }

    try
    {
        const crypto = require('crypto');
        const secret = process.env.QR_SECRET || 'visitor-management-secret-key';

        const qrData = {
      visitId: visitId,
      timestamp: Date.now(),
      signature: crypto
        .createHmac('sha256', secret)
        .update(`${ visitId}:${ Date.now()}`)
        .digest('hex')
        .substring(0, 16)
    };

return {
success: true,
      qrData: JSON.stringify(qrData),
      qrString: Buffer.from(JSON.stringify(qrData)).toString('base64')
    }
;
  } catch (error) {
    console.error('Error generating QR code:', error);
    throw new functions.https.HttpsError('internal', error.message);
}
}

/**
 * Gets visitor statistics for analytics
 */
async function getVisitorStats(data, context, db)
{
    if (!context.auth)
    {
        throw new functions.https.HttpsError('unauthenticated', 'User must be authenticated');
    }

    const { startDate, endDate, visitorId } = data;

    try
    {
        let query = db.collection('visits');

        if (visitorId)
        {
            query = query.where('visitorId', '==', visitorId);
        }

        if (startDate)
        {
            query = query.where('visitDate', '>=', new Date(startDate));
        }

        if (endDate)
        {
            query = query.where('visitDate', '<=', new Date(endDate));
        }

        const visitsSnapshot = await query.get();

        const stats = {
      totalVisits: visitsSnapshot.size,
      approved: 0,
      pending: 0,
      denied: 0,
      checkIns: 0,
      companies: new Set(),
      hosts: new Set()
    };

const visitIds = [];

visitsSnapshot.forEach(doc => {
    const visit = doc.data();
    visitIds.push(doc.id);

    if (visit.status === 'approved') stats.approved++;
    else if (visit.status === 'pending') stats.pending++;
    else if (visit.status === 'denied') stats.denied++;

    if (visit.visitorCompany) stats.companies.add(visit.visitorCompany);
    if (visit.hostName) stats.hosts.add(visit.hostName);
});

// Count check-ins for these visits
if (visitIds.length > 0)
{
    const checkInsSnapshot = await db.collection('qr_checkins')
      .where('visitId', 'in', visitIds.slice(0, 10)) // Firestore limit
      .get();

    stats.checkIns = checkInsSnapshot.size;
}

return {
success: true,
      stats:
    {
        ...stats,
        uniqueCompanies: stats.companies.size,
        uniqueHosts: stats.hosts.size,
        companies: Array.from(stats.companies),
        hosts: Array.from(stats.hosts)
      }
}
;
  } catch (error) {
    console.error('Error getting visitor stats:', error);
    throw new functions.https.HttpsError('internal', error.message);
}
}

/**
 * Bulk approve visits (admin function)
 */
async function bulkApproveVisits(data, context, db, messaging)
{
    // Check authentication
    if (!context.auth)
    {
        throw new functions.https.HttpsError('unauthenticated', 'User must be authenticated');
    }

    // Check admin role
    const adminDoc = await db.collection('admins').doc(context.auth.uid).get();
    if (!adminDoc.exists || !adminDoc.data().isActive)
    {
        throw new functions.https.HttpsError('permission-denied', 'Admin access required');
    }

    const { visitIds } = data;

    if (!visitIds || !Array.isArray(visitIds) || visitIds.length === 0)
    {
        throw new functions.https.HttpsError('invalid-argument', 'visitIds array is required');
    }

    if (visitIds.length > 50)
    {
        throw new functions.https.HttpsError('invalid-argument', 'Maximum 50 visits can be approved at once');
    }

    try
    {
        const batch = db.batch();
        const results = [];

        for (const visitId of visitIds) {
            const visitRef = db.collection('visits').doc(visitId);
            const visitDoc = await visitRef.get();

            if (!visitDoc.exists)
            {
                results.push({ visitId, success: false, error: 'Visit not found' });
                continue;
            }

            const visit = visitDoc.data();

            if (visit.status !== 'pending')
            {
                results.push({ visitId, success: false, error: `Visit is ${ visit.status}` });
                continue;
            }

            // Generate QR code
            const crypto = require('crypto');
            const qrData = {
        visitId: visitId,
        visitorId: visit.visitorId,
        timestamp: Date.now(),
        signature: crypto
          .createHmac('sha256', process.env.QR_SECRET || 'visitor-management-secret-key')
          .update(`${ visitId}:${ visit.visitorId}`)
          .digest('hex')
          .substring(0, 16)
      };

batch.update(visitRef, {
status: 'approved',
        approvedBy: context.auth.uid,
        approvedAt: admin.firestore.FieldValue.serverTimestamp(),
        updatedAt: admin.firestore.FieldValue.serverTimestamp(),
        qrCode: JSON.stringify(qrData)
      });

// Send notification
await notifications.sendVisitApprovedNotification(visitId, db, messaging);

results.push({ visitId, success: true });
    }

    await batch.commit();

const successCount = results.filter(r => r.success).length;
console.log(`Bulk approved ${ successCount}
visits`);

return {
success: true,
      approved: successCount,
      failed: results.length - successCount,
      results: results
    }
;
  } catch (error) {
    console.error('Error in bulk approve:', error);
    throw new functions.https.HttpsError('internal', error.message);
}
}

/**
 * Exports visit data to CSV
 */
async function exportVisitData(req, res, db)
{
    try
    {
        // Basic authentication check (you should implement proper auth)
        const authHeader = req.headers.authorization;
        if (!authHeader || !authHeader.startsWith('Bearer '))
        {
            res.status(401).send('Unauthorized');
            return;
        }

        const { startDate, endDate } = req.query;

        let query = db.collection('visits');

        if (startDate)
        {
            query = query.where('visitDate', '>=', new Date(startDate));
        }

        if (endDate)
        {
            query = query.where('visitDate', '<=', new Date(endDate));
        }

        const visitsSnapshot = await query.orderBy('visitDate', 'desc').limit(1000).get();

        // Generate CSV
        let csv = 'Visit Date,Visitor Name,Email,Company,Phone,Host Name,Department,Purpose,Status,Approved By,Approval Date\n';

        visitsSnapshot.forEach(doc => {
            const visit = doc.data();
            const visitDate = visit.visitDate.toDate().toISOString().split('T')[0];
            const approvalDate = visit.approvedAt ? visit.approvedAt.toDate().toISOString() : '';

            csv += `"${visitDate}","${visit.visitorName}","${visit.visitorEmail}","${visit.visitorCompany}","${visit.visitorPhone}","${visit.hostName}","${visit.hostDepartment}","${visit.purposeOfVisit}","${visit.status}","${visit.approvedBy || ''}","${approvalDate}"\n`;
        });

        res.setHeader('Content-Type', 'text/csv');
        res.setHeader('Content-Disposition', `attachment; filename = "visits_${Date.now()}.csv"`);
        res.status(200).send(csv);

    }
    catch (error)
    {
        console.error('Error exporting data:', error);
        res.status(500).send('Export failed');
    }
}

/**
 * Cleans up old activity logs (older than 90 days)
 */
async function cleanupOldActivityLogs(db)
{
    try
    {
        const ninetyDaysAgo = new Date();
        ninetyDaysAgo.setDate(ninetyDaysAgo.getDate() - 90);

        const oldLogs = await db.collection('activity_logs')
          .where('timestamp', '<', ninetyDaysAgo.toISOString())
          .limit(500)
          .get();

        if (oldLogs.empty)
        {
            console.log('No old activity logs to clean up');
            return null;
        }

        const batch = db.batch();
        oldLogs.forEach(doc => {
            batch.delete(doc.ref);
        });

        await batch.commit();
        console.log(`Deleted ${ oldLogs.size}
        old activity logs`);

        return { deleted: oldLogs.size }
        ;
    }
    catch (error)
    {
        console.error('Error cleaning up activity logs:', error);
        throw error;
    }
}

module.exports = {
    cleanupOldNotifications,
  autoExpirePendingVisits,
  generateQRCode,
  getVisitorStats,
  bulkApproveVisits,
  exportVisitData,
  cleanupOldActivityLogs
}
;
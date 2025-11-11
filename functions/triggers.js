const notifications = require('./notifications');

/**
 * Triggered when a new visit is created
 * Notifies admins about the new visit request
 */
async function onVisitCreated(snap, context, db, messaging) {
    try {
        const visit = snap.data();
        const visitId = context.params.visitId;

        console.log('New visit created:', visitId);

        // Only notify for pending visits
        if (visit.status === 'pending') {
            await notifications.notifyAdminsNewVisit(visitId, db, messaging);
        }

        // Log the event
        await db.collection('activity_logs').add({
            type: 'visit_created',
            visitId: visitId,
            visitorId: visit.visitorId,
            visitorName: visit.visitorName,
            timestamp: new Date().toISOString(),
            status: visit.status
        });

        return null;
    } catch (error) {
        console.error('Error in onVisitCreated:', error);
        return null;
    }
}

/**
 * Triggered when a visit status changes
 * Sends appropriate notifications to visitor
 */
async function onVisitStatusChanged(change, context, db, messaging) {
    try {
        const beforeData = change.before.data();
        const afterData = change.after.data();
        const visitId = context.params.visitId;

        // Check if status changed
        if (beforeData.status === afterData.status) {
            return null;
        }

        console.log(`Visit ${visitId} status changed: ${beforeData.status} -> ${afterData.status}`);

        // Handle status changes
        if (afterData.status === 'approved') {
            // Generate QR code data
            const qrData = {
                visitId: visitId,
                visitorId: afterData.visitorId,
                timestamp: new Date().getTime(),
                signature: generateSignature(visitId, afterData.visitorId)
            };

            // Update visit with QR code
            await change.after.ref.update({
                qrCode: JSON.stringify(qrData)
            });

            // Send approval notification
            await notifications.sendVisitApprovedNotification(visitId, db, messaging);

        } else if (afterData.status === 'denied') {
            // Send denial notification
            const reason = afterData.denialReason || 'No reason provided';
            await notifications.sendVisitDeniedNotification(visitId, reason, db, messaging);
        }

        // Log status change
        await db.collection('activity_logs').add({
            type: 'visit_status_changed',
            visitId: visitId,
            visitorId: afterData.visitorId,
            oldStatus: beforeData.status,
            newStatus: afterData.status,
            changedBy: afterData.approvedBy,
            timestamp: new Date().toISOString()
        });

        return null;
    } catch (error) {
        console.error('Error in onVisitStatusChanged:', error);
        return null;
    }
}

/**
 * Triggered when a check-in is created
 * Sends confirmation notification
 */
async function onCheckInCreated(snap, context, db, messaging) {
    try {
        const checkIn = snap.data();
        const checkInId = context.params.checkInId;

        console.log('Check-in created:', checkInId);

        // Get visit details
        const visitDoc = await db.collection('visits').doc(checkIn.visitId).get();

        if (visitDoc.exists) {
            const visit = visitDoc.data();

            // Note: Notification is already sent by validateQRCode function
            // This is a backup/logging mechanism

            // Update visit with last check-in
            await visitDoc.ref.update({
                lastCheckInId: checkInId,
                lastCheckInTime: checkIn.checkInTime
            });

            // Log check-in
            await db.collection('activity_logs').add({
                type: 'check_in',
                checkInId: checkInId,
                visitId: checkIn.visitId,
                visitorId: checkIn.visitorId,
                location: checkIn.checkInLocation,
                timestamp: new Date().toISOString()
            });
        }

        return null;
    } catch (error) {
        console.error('Error in onCheckInCreated:', error);
        return null;
    }
}

/**
 * Triggered when a new visitor registers
 * Sends welcome notification
 */
async function onVisitorCreated(snap, context, messaging) {
    try {
        const visitor = snap.data();
        const visitorId = context.params.visitorId;

        console.log('New visitor registered:', visitorId);

        // Send welcome notification
        await notifications.sendWelcomeNotification(
            visitorId,
            visitor.fullName,
            snap.ref.firestore,
            messaging
        );

        return null;
    } catch (error) {
        console.error('Error in onVisitorCreated:', error);
        return null;
    }
}

/**
 * Triggered when a visitor updates their FCM token
 * This helps maintain up-to-date notification delivery
 */
async function onVisitorUpdated(change, context, db) {
    try {
        const beforeData = change.before.data();
        const afterData = change.after.data();

        // Check if FCM token changed
        if (beforeData.fcmToken !== afterData.fcmToken) {
            console.log(`FCM token updated for visitor: ${context.params.visitorId}`);

            // Log token update
            await db.collection('activity_logs').add({
                type: 'fcm_token_updated',
                visitorId: context.params.visitorId,
                timestamp: new Date().toISOString()
            });
        }

        return null;
    } catch (error) {
        console.error('Error in onVisitorUpdated:', error);
        return null;
    }
}

/**
 * Triggered when check-in is updated (check-out)
 * Logs the check-out activity
 */
async function onCheckInUpdated(change, context, db, messaging) {
    try {
        const beforeData = change.before.data();
        const afterData = change.after.data();

        // Check if check-out time was added
        if (!beforeData.checkOutTime && afterData.checkOutTime) {
            console.log(`Visitor checked out: ${context.params.checkInId}`);

            // Calculate duration
            const checkInTime = afterData.checkInTime.toDate();
            const checkOutTime = afterData.checkOutTime.toDate();
            const durationMinutes = Math.floor((checkOutTime - checkInTime) / (1000 * 60));

            // Log check-out
            await db.collection('activity_logs').add({
                type: 'check_out',
                checkInId: context.params.checkInId,
                visitId: afterData.visitId,
                visitorId: afterData.visitorId,
                durationMinutes: durationMinutes,
                timestamp: new Date().toISOString()
            });

            // Update visit with check-out info
            await db.collection('visits').doc(afterData.visitId).update({
                lastCheckOutTime: afterData.checkOutTime,
                visitDuration: durationMinutes
            });
        }

        return null;
    } catch (error) {
        console.error('Error in onCheckInUpdated:', error);
        return null;
    }
}

/**
 * Generates a signature for QR code security
 */
function generateSignature(visitId, visitorId) {
    const crypto = require('crypto');
    const secret = process.env.QR_SECRET || 'visitor-management-secret-key';
    const data = `${visitId}:${visitorId}:${Date.now()}`;

    return crypto
        .createHmac('sha256', secret)
        .update(data)
        .digest('hex')
        .substring(0, 16);
}

module.exports = {
    onVisitCreated,
    onVisitStatusChanged,
    onCheckInCreated,
    onVisitorCreated,
    onVisitorUpdated,
    onCheckInUpdated
};
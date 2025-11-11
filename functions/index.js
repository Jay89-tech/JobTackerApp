const functions = require('firebase-functions');
const admin = require('firebase-admin');

// Initialize Firebase Admin
admin.initializeApp();

const db = admin.firestore();
const messaging = admin.messaging();

// Import function modules
const qrValidation = require('./qr-validation');
const notifications = require('./notifications');
const triggers = require('./triggers');
const utilities = require('./utilities');

// ============================================
// QR CODE VALIDATION FUNCTIONS
// ============================================

/**
 * Validates QR code and creates check-in record
 * Callable function from mobile app
 */
exports.validateQRCode = functions.https.onCall(async (data, context) => {
    return await qrValidation.validateQRCode(data, context, db, messaging);
});

/**
 * Checks out a visitor
 * Callable function from mobile app or admin
 */
exports.checkOutVisitor = functions.https.onCall(async (data, context) => {
    return await qrValidation.checkOutVisitor(data, context, db, messaging);
});

/**
 * Verifies if a visit is valid for check-in
 * Callable function for pre-validation
 */
exports.verifyVisit = functions.https.onCall(async (data, context) => {
    return await qrValidation.verifyVisit(data, context, db);
});

// ============================================
// AUTOMATED NOTIFICATION TRIGGERS
// ============================================

/**
 * Triggers when a visit is created
 * Sends notification to admins
 */
exports.onVisitCreated = functions.firestore
    .document('visits/{visitId}')
    .onCreate(async (snap, context) => {
        return await triggers.onVisitCreated(snap, context, db, messaging);
    });

/**
 * Triggers when a visit status changes
 * Sends notifications to visitor
 */
exports.onVisitStatusChanged = functions.firestore
    .document('visits/{visitId}')
    .onUpdate(async (change, context) => {
        return await triggers.onVisitStatusChanged(change, context, db, messaging);
    });

/**
 * Triggers when a check-in is created
 * Sends confirmation notification
 */
exports.onCheckInCreated = functions.firestore
    .document('qr_checkins/{checkInId}')
    .onCreate(async (snap, context) => {
        return await triggers.onCheckInCreated(snap, context, db, messaging);
    });

/**
 * Triggers when a visitor registers
 * Sends welcome notification
 */
exports.onVisitorCreated = functions.firestore
    .document('visitors/{visitorId}')
    .onCreate(async (snap, context) => {
        return await triggers.onVisitorCreated(snap, context, messaging);
    });

// ============================================
// SCHEDULED FUNCTIONS
// ============================================

/**
 * Sends reminder notifications for upcoming visits
 * Runs every hour
 */
exports.sendUpcomingVisitReminders = functions.pubsub
    .schedule('0 * * * *') // Every hour
    .timeZone('UTC')
    .onRun(async (context) => {
        return await notifications.sendUpcomingVisitReminders(db, messaging);
    });

/**
 * Sends daily summary to admins
 * Runs at 5 PM daily
 */
exports.sendDailySummary = functions.pubsub
    .schedule('0 17 * * *') // 5 PM UTC daily
    .timeZone('UTC')
    .onRun(async (context) => {
        return await notifications.sendDailySummary(db, messaging);
    });

/**
 * Cleans up old notifications
 * Runs daily at midnight
 */
exports.cleanupOldNotifications = functions.pubsub
    .schedule('0 0 * * *') // Midnight UTC
    .timeZone('UTC')
    .onRun(async (context) => {
        return await utilities.cleanupOldNotifications(db);
    });

/**
 * Auto-deny expired pending visits
 * Runs every 6 hours
 */
exports.autoExpirePendingVisits = functions.pubsub
    .schedule('0 */6 * * *') // Every 6 hours
    .timeZone('UTC')
    .onRun(async (context) => {
        return await utilities.autoExpirePendingVisits(db, messaging);
    });

// ============================================
// UTILITY FUNCTIONS
// ============================================

/**
 * Generates QR code data for approved visits
 * HTTP endpoint for manual trigger
 */
exports.generateQRCode = functions.https.onCall(async (data, context) => {
    return await utilities.generateQRCode(data, context);
});

/**
 * Gets visitor statistics
 * Callable function for analytics
 */
exports.getVisitorStats = functions.https.onCall(async (data, context) => {
    return await utilities.getVisitorStats(data, context, db);
});

/**
 * Bulk approve visits (admin function)
 * Callable function for batch operations
 */
exports.bulkApproveVisits = functions.https.onCall(async (data, context) => {
    return await utilities.bulkApproveVisits(data, context, db, messaging);
});

/**
 * Export visit data to CSV
 * HTTP endpoint with authentication
 */
exports.exportVisitData = functions.https.onRequest(async (req, res) => {
    return await utilities.exportVisitData(req, res, db);
});

// ============================================
// ADMIN MANAGEMENT FUNCTIONS
// ============================================

/**
 * Creates a new admin user
 * Callable function (superadmin only)
 */
exports.createAdmin = functions.https.onCall(async (data, context) => {
    // Check if caller is superadmin
    if (!context.auth) {
        throw new functions.https.HttpsError('unauthenticated', 'User must be authenticated');
    }

    const callerDoc = await db.collection('admins').doc(context.auth.uid).get();
    if (!callerDoc.exists || callerDoc.data().role !== 'superadmin') {
        throw new functions.https.HttpsError('permission-denied', 'Only superadmins can create admins');
    }

    try {
        // Create Firebase Auth user
        const userRecord = await admin.auth().createUser({
            email: data.email,
            password: data.password,
            displayName: data.fullName,
            emailVerified: true
        });

        // Set custom claims
        await admin.auth().setCustomUserClaims(userRecord.uid, {
            role: data.role || 'admin',
            isAdmin: true
        });

        // Create Firestore document
        await db.collection('admins').doc(userRecord.uid).set({
            email: data.email,
            fullName: data.fullName,
            role: data.role || 'admin',
            department: data.department || '',
            isActive: true,
            createdAt: admin.firestore.FieldValue.serverTimestamp(),
            lastLoginAt: admin.firestore.FieldValue.serverTimestamp()
        });

        return {
            success: true,
            uid: userRecord.uid,
            message: 'Admin user created successfully'
        };
    } catch (error) {
        console.error('Error creating admin:', error);
        throw new functions.https.HttpsError('internal', error.message);
    }
});

/**
 * Health check endpoint
 */
exports.healthCheck = functions.https.onRequest((req, res) => {
    res.status(200).json({
        status: 'healthy',
        timestamp: new Date().toISOString(),
        version: '1.0.0'
    });
});
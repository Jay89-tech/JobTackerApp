const admin = require('firebase-admin');

/**
 * Sends reminder notifications for upcoming visits
 * Runs hourly to notify visitors 2 hours before their scheduled visit
 */
async function sendUpcomingVisitReminders(db, messaging) {
    try {
        const now = new Date();
        const twoHoursLater = new Date(now.getTime() + (2 * 60 * 60 * 1000));
        const twoHoursFifteenLater = new Date(now.getTime() + (2.25 * 60 * 60 * 1000));

        // Get visits scheduled between 2 and 2.25 hours from now
        const upcomingVisits = await db.collection('visits')
            .where('status', '==', 'approved')
            .where('expectedArrivalTime', '>=', twoHoursLater)
            .where('expectedArrivalTime', '<=', twoHoursFifteenLater)
            .get();

        if (upcomingVisits.empty) {
            console.log('No upcoming visits in the next 2 hours');
            return null;
        }

        const notifications = [];

        for (const visitDoc of upcomingVisits.docs) {
            const visit = visitDoc.data();

            // Get visitor details
            const visitorDoc = await db.collection('visitors').doc(visit.visitorId).get();

            if (!visitorDoc.exists || !visitorDoc.data().fcmToken) {
                continue;
            }

            const visitor = visitorDoc.data();
            const visitTime = visit.expectedArrivalTime.toDate();

            const message = {
                token: visitor.fcmToken,
                notification: {
                    title: 'Visit Reminder',
                    body: `Your visit is scheduled in 2 hours at ${visitTime.toLocaleTimeString('en-US', { hour: 'numeric', minute: '2-digit' })}`
                },
                data: {
                    type: 'visit_reminder',
                    visitId: visitDoc.id,
                    visitTime: visitTime.toISOString()
                },
                android: {
                    priority: 'high',
                    notification: {
                        sound: 'default',
                        color: '#3b82f6',
                        channelId: 'visit_reminders'
                    }
                },
                apns: {
                    payload: {
                        aps: {
                            sound: 'default',
                            badge: 1
                        }
                    }
                }
            };

            try {
                await messaging.send(message);

                // Save notification
                await db.collection('notifications').add({
                    userId: visit.visitorId,
                    title: 'Visit Reminder',
                    message: `Your visit is scheduled in 2 hours at ${visitTime.toLocaleTimeString()}`,
                    type: 'visit_reminder',
                    relatedVisitId: visitDoc.id,
                    isRead: false,
                    createdAt: admin.firestore.FieldValue.serverTimestamp()
                });

                notifications.push(visitDoc.id);
            } catch (error) {
                console.error(`Error sending reminder for visit ${visitDoc.id}:`, error);
            }
        }

        console.log(`Sent ${notifications.length} visit reminders`);
        return { sent: notifications.length, visitIds: notifications };

    } catch (error) {
        console.error('Error in sendUpcomingVisitReminders:', error);
        throw error;
    }
}

/**
 * Sends daily summary to all active admins
 * Runs at 5 PM daily with statistics
 */
async function sendDailySummary(db, messaging) {
    try {
        const today = new Date();
        today.setHours(0, 0, 0, 0);
        const tomorrow = new Date(today);
        tomorrow.setDate(tomorrow.getDate() + 1);

        // Get today's statistics
        const todayVisits = await db.collection('visits')
            .where('visitDate', '>=', today)
            .where('visitDate', '<', tomorrow)
            .get();

        const todayCheckIns = await db.collection('qr_checkins')
            .where('checkInTime', '>=', today)
            .where('checkInTime', '<', tomorrow)
            .get();

        const stats = {
            totalVisits: todayVisits.size,
            approved: 0,
            pending: 0,
            denied: 0,
            checkIns: todayCheckIns.size
        };

        todayVisits.forEach(doc => {
            const status = doc.data().status;
            if (status === 'approved') stats.approved++;
            else if (status === 'pending') stats.pending++;
            else if (status === 'denied') stats.denied++;
        });

        // Get all active admins
        const admins = await db.collection('admins')
            .where('isActive', '==', true)
            .get();

        const notifications = [];

        for (const adminDoc of admins.docs) {
            const admin = adminDoc.data();

            if (!admin.fcmToken) {
                continue;
            }

            const message = {
                token: admin.fcmToken,
                notification: {
                    title: 'Daily Summary',
                    body: `Today: ${stats.totalVisits} visits, ${stats.checkIns} check-ins, ${stats.pending} pending approvals`
                },
                data: {
                    type: 'daily_summary',
                    totalVisits: stats.totalVisits.toString(),
                    checkIns: stats.checkIns.toString(),
                    pending: stats.pending.toString(),
                    date: today.toISOString()
                },
                android: {
                    priority: 'default',
                    notification: {
                        channelId: 'daily_reports'
                    }
                }
            };

            try {
                await messaging.send(message);
                notifications.push(adminDoc.id);
            } catch (error) {
                console.error(`Error sending summary to admin ${adminDoc.id}:`, error);
            }
        }

        console.log(`Sent daily summary to ${notifications.length} admins`);
        return { sent: notifications.length, stats: stats };

    } catch (error) {
        console.error('Error in sendDailySummary:', error);
        throw error;
    }
}

/**
 * Sends notification when visit is approved
 */
async function sendVisitApprovedNotification(visitId, db, messaging) {
    try {
        const visitDoc = await db.collection('visits').doc(visitId).get();

        if (!visitDoc.exists) {
            console.error('Visit not found:', visitId);
            return;
        }

        const visit = visitDoc.data();
        const visitorDoc = await db.collection('visitors').doc(visit.visitorId).get();

        if (!visitorDoc.exists || !visitorDoc.data().fcmToken) {
            console.log('No FCM token for visitor:', visit.visitorId);
            return;
        }

        const visitor = visitorDoc.data();
        const visitDate = visit.visitDate.toDate();

        const message = {
            token: visitor.fcmToken,
            notification: {
                title: 'Visit Approved',
                body: `Your visit request for ${visitDate.toLocaleDateString()} has been approved!`
            },
            data: {
                type: 'visit_approved',
                visitId: visitId,
                visitDate: visitDate.toISOString()
            },
            android: {
                priority: 'high',
                notification: {
                    sound: 'default',
                    color: '#10b981',
                    channelId: 'visit_updates'
                }
            },
            apns: {
                payload: {
                    aps: {
                        sound: 'default',
                        badge: 1
                    }
                }
            }
        };

        await messaging.send(message);

        // Save notification
        await db.collection('notifications').add({
            userId: visit.visitorId,
            title: 'Visit Approved',
            message: `Your visit request for ${visitDate.toLocaleDateString()} has been approved!`,
            type: 'visit_approved',
            relatedVisitId: visitId,
            isRead: false,
            createdAt: admin.firestore.FieldValue.serverTimestamp()
        });

        console.log('Visit approval notification sent for:', visitId);

    } catch (error) {
        console.error('Error sending visit approval notification:', error);
    }
}

/**
 * Sends notification when visit is denied
 */
async function sendVisitDeniedNotification(visitId, reason, db, messaging) {
    try {
        const visitDoc = await db.collection('visits').doc(visitId).get();

        if (!visitDoc.exists) {
            return;
        }

        const visit = visitDoc.data();
        const visitorDoc = await db.collection('visitors').doc(visit.visitorId).get();

        if (!visitorDoc.exists || !visitorDoc.data().fcmToken) {
            return;
        }

        const visitor = visitorDoc.data();

        const message = {
            token: visitor.fcmToken,
            notification: {
                title: 'Visit Request Denied',
                body: `Your visit request has been denied. Reason: ${reason}`
            },
            data: {
                type: 'visit_denied',
                visitId: visitId,
                reason: reason
            },
            android: {
                priority: 'high',
                notification: {
                    sound: 'default',
                    color: '#ef4444',
                    channelId: 'visit_updates'
                }
            }
        };

        await messaging.send(message);

        await db.collection('notifications').add({
            userId: visit.visitorId,
            title: 'Visit Request Denied',
            message: `Your visit request has been denied. Reason: ${reason}`,
            type: 'visit_denied',
            relatedVisitId: visitId,
            isRead: false,
            createdAt: admin.firestore.FieldValue.serverTimestamp()
        });

    } catch (error) {
        console.error('Error sending visit denial notification:', error);
    }
}

/**
 * Sends notification to admins when new visit is requested
 */
async function notifyAdminsNewVisit(visitId, db, messaging) {
    try {
        const visitDoc = await db.collection('visits').doc(visitId).get();

        if (!visitDoc.exists) {
            return;
        }

        const visit = visitDoc.data();
        const visitDate = visit.visitDate.toDate();

        // Get all active admins
        const admins = await db.collection('admins')
            .where('isActive', '==', true)
            .get();

        for (const adminDoc of admins.docs) {
            const admin = adminDoc.data();

            if (!admin.fcmToken) {
                continue;
            }

            const message = {
                token: admin.fcmToken,
                notification: {
                    title: 'New Visit Request',
                    body: `${visit.visitorName} from ${visit.visitorCompany} - ${visitDate.toLocaleDateString()}`
                },
                data: {
                    type: 'new_visit_request',
                    visitId: visitId,
                    visitorName: visit.visitorName,
                    company: visit.visitorCompany
                },
                android: {
                    priority: 'high',
                    notification: {
                        sound: 'default',
                        channelId: 'admin_alerts'
                    }
                }
            };

            try {
                await messaging.send(message);
            } catch (error) {
                console.error(`Error notifying admin ${adminDoc.id}:`, error);
            }
        }

    } catch (error) {
        console.error('Error notifying admins:', error);
    }
}

/**
 * Sends welcome notification to new visitors
 */
async function sendWelcomeNotification(visitorId, visitorName, db, messaging) {
    try {
        const visitorDoc = await db.collection('visitors').doc(visitorId).get();

        if (!visitorDoc.exists || !visitorDoc.data().fcmToken) {
            return;
        }

        const fcmToken = visitorDoc.data().fcmToken;

        const message = {
            token: fcmToken,
            notification: {
                title: 'Welcome to Visitor Management System',
                body: `Hello ${visitorName}! Your account has been created successfully.`
            },
            data: {
                type: 'welcome',
                visitorId: visitorId
            },
            android: {
                priority: 'default',
                notification: {
                    sound: 'default',
                    color: '#3b82f6'
                }
            }
        };

        await messaging.send(message);

        await db.collection('notifications').add({
            userId: visitorId,
            title: 'Welcome to Visitor Management System',
            message: `Hello ${visitorName}! Your account has been created successfully.`,
            type: 'welcome',
            isRead: false,
            createdAt: admin.firestore.FieldValue.serverTimestamp()
        });

    } catch (error) {
        console.error('Error sending welcome notification:', error);
    }
}

module.exports = {
    sendUpcomingVisitReminders,
    sendDailySummary,
    sendVisitApprovedNotification,
    sendVisitDeniedNotification,
    notifyAdminsNewVisit,
    sendWelcomeNotification
};
using Google.Cloud.Firestore;
using FirebaseAdmin.Messaging;
using VisitorManagement.Models;

namespace VisitorManagement.Services
{
    public interface INotificationService
    {
        Task SendVisitApprovedNotificationAsync(Visit visit);
        Task SendVisitDeniedNotificationAsync(Visit visit, string reason);
        Task SendCheckInSuccessNotificationAsync(string visitorId, string visitId);
    }

    public class NotificationService : INotificationService
    {
        private readonly FirestoreDb _firestore;

        public NotificationService(IFirebaseService firebaseService)
        {
            _firestore = firebaseService.GetFirestore();
        }

        public async Task SendVisitApprovedNotificationAsync(Visit visit)
        {
            try
            {
                // Get visitor FCM token
                var visitorDoc = await _firestore.Collection("visitors")
                    .Document(visit.VisitorId)
                    .GetSnapshotAsync();

                if (!visitorDoc.Exists)
                    return;

                var visitor = visitorDoc.ConvertTo<Visitor>();

                if (string.IsNullOrEmpty(visitor.FcmToken))
                    return;

                // Send FCM notification
                var message = new Message()
                {
                    Token = visitor.FcmToken,
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = "Visit Approved",
                        Body = $"Your visit request for {visit.VisitDate:MMM dd, yyyy} has been approved."
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "type", "visit_approved" },
                        { "visitId", visit.Id }
                    }
                };

                await FirebaseMessaging.DefaultInstance.SendAsync(message);

                // Save notification to Firestore
                await SaveNotificationAsync(
                    visit.VisitorId,
                    "Visit Approved",
                    $"Your visit request for {visit.VisitDate:MMM dd, yyyy} has been approved.",
                    "visit_approved",
                    visit.Id
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }

        public async Task SendVisitDeniedNotificationAsync(Visit visit, string reason)
        {
            try
            {
                var visitorDoc = await _firestore.Collection("visitors")
                    .Document(visit.VisitorId)
                    .GetSnapshotAsync();

                if (!visitorDoc.Exists)
                    return;

                var visitor = visitorDoc.ConvertTo<Visitor>();

                if (string.IsNullOrEmpty(visitor.FcmToken))
                    return;

                var message = new Message()
                {
                    Token = visitor.FcmToken,
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = "Visit Denied",
                        Body = $"Your visit request has been denied. Reason: {reason}"
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "type", "visit_denied" },
                        { "visitId", visit.Id }
                    }
                };

                await FirebaseMessaging.DefaultInstance.SendAsync(message);

                await SaveNotificationAsync(
                    visit.VisitorId,
                    "Visit Denied",
                    $"Your visit request has been denied. Reason: {reason}",
                    "visit_denied",
                    visit.Id
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }

        public async Task SendCheckInSuccessNotificationAsync(string visitorId, string visitId)
        {
            try
            {
                var visitorDoc = await _firestore.Collection("visitors")
                    .Document(visitorId)
                    .GetSnapshotAsync();

                if (!visitorDoc.Exists)
                    return;

                var visitor = visitorDoc.ConvertTo<Visitor>();

                if (string.IsNullOrEmpty(visitor.FcmToken))
                    return;

                var message = new Message()
                {
                    Token = visitor.FcmToken,
                    Notification = new FirebaseAdmin.Messaging.Notification()
                    {
                        Title = "Check-In Successful",
                        Body = "You have successfully checked in. Welcome!"
                    },
                    Data = new Dictionary<string, string>()
                    {
                        { "type", "check_in_success" },
                        { "visitId", visitId }
                    }
                };

                await FirebaseMessaging.DefaultInstance.SendAsync(message);

                await SaveNotificationAsync(
                    visitorId,
                    "Check-In Successful",
                    "You have successfully checked in. Welcome!",
                    "check_in_success",
                    visitId
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error sending notification: {ex.Message}");
            }
        }

        private async Task SaveNotificationAsync(
            string userId,
            string title,
            string message,
            string type,
            string relatedVisitId)
        {
            var notification = new UserNotification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Type = type,
                RelatedVisitId = relatedVisitId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _firestore.Collection("notifications").AddAsync(notification);
        }
    }
}
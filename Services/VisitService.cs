using Google.Cloud.Firestore;
using VisitorManagement.Models;

namespace VisitorManagement.Services
{
    public interface IVisitService
    {
        Task<List<Visit>> GetAllVisitsAsync();
        Task<List<Visit>> GetPendingVisitsAsync();
        Task<List<Visit>> GetTodayVisitsAsync();
        Task<Visit> GetVisitByIdAsync(string visitId);
        Task<bool> ApproveVisitAsync(string visitId, string adminId);
        Task<bool> DenyVisitAsync(string visitId, string adminId, string reason);
        Task<int> GetTotalVisitsTodayAsync();
        Task<int> GetPendingVisitsCountAsync();
        Task<int> GetApprovedVisitsCountAsync();
    }

    public class VisitService : IVisitService
    {
        private readonly FirestoreDb _firestore;
        private readonly INotificationService _notificationService;

        public VisitService(IFirebaseService firebaseService, INotificationService notificationService)
        {
            _firestore = firebaseService.GetFirestore();
            _notificationService = notificationService;
        }

        public async Task<List<Visit>> GetAllVisitsAsync()
        {
            var query = _firestore.Collection("visits")
                .OrderByDescending("CreatedAt")
                .Limit(100);

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents.Select(doc =>
            {
                var visit = doc.ConvertTo<Visit>();
                visit.Id = doc.Id;
                return visit;
            }).ToList();
        }

        public async Task<List<Visit>> GetPendingVisitsAsync()
        {
            var query = _firestore.Collection("visits")
                .WhereEqualTo("Status", "pending")
                .OrderBy("VisitDate");

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents.Select(doc =>
            {
                var visit = doc.ConvertTo<Visit>();
                visit.Id = doc.Id;
                return visit;
            }).ToList();
        }

        public async Task<List<Visit>> GetTodayVisitsAsync()
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var query = _firestore.Collection("visits")
                .WhereGreaterThanOrEqualTo("VisitDate", todayStart)
                .WhereLessThan("VisitDate", todayEnd)
                .OrderBy("VisitDate");

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents.Select(doc =>
            {
                var visit = doc.ConvertTo<Visit>();
                visit.Id = doc.Id;
                return visit;
            }).ToList();
        }

        public async Task<Visit> GetVisitByIdAsync(string visitId)
        {
            var docRef = _firestore.Collection("visits").Document(visitId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            var visit = snapshot.ConvertTo<Visit>();
            visit.Id = snapshot.Id;
            return visit;
        }

        public async Task<bool> ApproveVisitAsync(string visitId, string adminId)
        {
            try
            {
                var visitRef = _firestore.Collection("visits").Document(visitId);
                var updates = new Dictionary<string, object>
                {
                    { "Status", "approved" },
                    { "ApprovedBy", adminId },
                    { "ApprovedAt", DateTime.UtcNow },
                    { "UpdatedAt", DateTime.UtcNow }
                };

                await visitRef.UpdateAsync(updates);

                // Send notification to visitor
                var visit = await GetVisitByIdAsync(visitId);
                if (visit != null)
                {
                    await _notificationService.SendVisitApprovedNotificationAsync(visit);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error approving visit: {ex.Message}");
            }
        }

        public async Task<bool> DenyVisitAsync(string visitId, string adminId, string reason)
        {
            try
            {
                var visitRef = _firestore.Collection("visits").Document(visitId);
                var updates = new Dictionary<string, object>
                {
                    { "Status", "denied" },
                    { "ApprovedBy", adminId },
                    { "ApprovedAt", DateTime.UtcNow },
                    { "DenialReason", reason },
                    { "UpdatedAt", DateTime.UtcNow }
                };

                await visitRef.UpdateAsync(updates);

                // Send notification to visitor
                var visit = await GetVisitByIdAsync(visitId);
                if (visit != null)
                {
                    await _notificationService.SendVisitDeniedNotificationAsync(visit, reason);
                }

                return true;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error denying visit: {ex.Message}");
            }
        }

        public async Task<int> GetTotalVisitsTodayAsync()
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var query = _firestore.Collection("visits")
                .WhereGreaterThanOrEqualTo("VisitDate", todayStart)
                .WhereLessThan("VisitDate", todayEnd);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Count;
        }

        public async Task<int> GetPendingVisitsCountAsync()
        {
            var query = _firestore.Collection("visits")
                .WhereEqualTo("Status", "pending");

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Count;
        }

        public async Task<int> GetApprovedVisitsCountAsync()
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var query = _firestore.Collection("visits")
                .WhereEqualTo("Status", "approved")
                .WhereGreaterThanOrEqualTo("VisitDate", todayStart)
                .WhereLessThan("VisitDate", todayEnd);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Count;
        }
    }
}
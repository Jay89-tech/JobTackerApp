using Google.Cloud.Firestore;
using VisitorManagement.Models;

namespace VisitorManagement.Services
{
    public interface ICheckInService
    {
        Task<List<QrCheckIn>> GetTodayCheckInsAsync();
        Task<List<QrCheckIn>> GetRecentCheckInsAsync(int limit = 10);
        Task<int> GetCheckedInCountAsync();
    }

    public class CheckInService : ICheckInService
    {
        private readonly FirestoreDb _firestore;

        public CheckInService(IFirebaseService firebaseService)
        {
            _firestore = firebaseService.GetFirestore();
        }

        public async Task<List<QrCheckIn>> GetTodayCheckInsAsync()
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var query = _firestore.Collection("qr_checkins")
                .WhereGreaterThanOrEqualTo("CheckInTime", todayStart)
                .WhereLessThan("CheckInTime", todayEnd)
                .OrderByDescending("CheckInTime");

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents.Select(doc =>
            {
                var checkIn = doc.ConvertTo<QrCheckIn>();
                checkIn.Id = doc.Id;
                return checkIn;
            }).ToList();
        }

        public async Task<List<QrCheckIn>> GetRecentCheckInsAsync(int limit = 10)
        {
            var query = _firestore.Collection("qr_checkins")
                .OrderByDescending("CheckInTime")
                .Limit(limit);

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents.Select(doc =>
            {
                var checkIn = doc.ConvertTo<QrCheckIn>();
                checkIn.Id = doc.Id;
                return checkIn;
            }).ToList();
        }

        public async Task<int> GetCheckedInCountAsync()
        {
            var todayStart = DateTime.Today;
            var todayEnd = todayStart.AddDays(1);

            var query = _firestore.Collection("qr_checkins")
                .WhereGreaterThanOrEqualTo("CheckInTime", todayStart)
                .WhereLessThan("CheckInTime", todayEnd)
                .WhereEqualTo("CheckOutTime", null);

            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Count;
        }
    }
}
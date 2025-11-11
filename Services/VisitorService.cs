using Google.Cloud.Firestore;
using VisitorManagement.Models;

namespace VisitorManagement.Services
{
    public interface IVisitorService
    {
        Task<List<Visitor>> GetAllVisitorsAsync();
        Task<Visitor> GetVisitorByIdAsync(string visitorId);
        Task<Visitor> GetVisitorByEmailAsync(string email);
    }

    public class VisitorService : IVisitorService
    {
        private readonly FirestoreDb _firestore;

        public VisitorService(IFirebaseService firebaseService)
        {
            _firestore = firebaseService.GetFirestore();
        }

        public async Task<List<Visitor>> GetAllVisitorsAsync()
        {
            var query = _firestore.Collection("visitors")
                .OrderByDescending("CreatedAt")
                .Limit(100);

            var snapshot = await query.GetSnapshotAsync();

            return snapshot.Documents.Select(doc =>
            {
                var visitor = doc.ConvertTo<Visitor>();
                visitor.Id = doc.Id;
                return visitor;
            }).ToList();
        }

        public async Task<Visitor> GetVisitorByIdAsync(string visitorId)
        {
            var docRef = _firestore.Collection("visitors").Document(visitorId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            var visitor = snapshot.ConvertTo<Visitor>();
            visitor.Id = snapshot.Id;
            return visitor;
        }

        public async Task<Visitor> GetVisitorByEmailAsync(string email)
        {
            var query = _firestore.Collection("visitors")
                .WhereEqualTo("Email", email)
                .Limit(1);

            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
                return null;

            var doc = snapshot.Documents[0];
            var visitor = doc.ConvertTo<Visitor>();
            visitor.Id = doc.Id;
            return visitor;
        }
    }
}
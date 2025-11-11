using Google.Cloud.Firestore;
using FirebaseAdmin.Auth;
using VisitorManagement.Models;

namespace VisitorManagement.Services
{
    public interface IFirebaseService
    {
        FirebaseAuth GetAuth();
        FirestoreDb GetFirestore();
        Task<Admin> GetAdminByEmailAsync(string email);
        Task UpdateLastLoginAsync(string adminId);
    }

    public class FirebaseService : IFirebaseService
    {
        private readonly FirestoreDb _firestore;
        private readonly FirebaseAuth _auth;

        public FirebaseService()
        {
            _firestore = FirestoreDb.Create("visitor-d5ed4");
            _auth = FirebaseAuth.DefaultInstance;
        }

        public FirebaseAuth GetAuth() => _auth;
        public FirestoreDb GetFirestore() => _firestore;

        public async Task<Admin> GetAdminByEmailAsync(string email)
        {
            try
            {
                var query = _firestore.Collection("admins")
                    .WhereEqualTo("Email", email)
                    .WhereEqualTo("IsActive", true)
                    .Limit(1);

                var snapshot = await query.GetSnapshotAsync();

                if (snapshot.Documents.Count == 0)
                    return null;

                var doc = snapshot.Documents[0];
                var admin = doc.ConvertTo<Admin>();
                admin.Id = doc.Id;
                return admin;
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching admin: {ex.Message}");
            }
        }

        public async Task UpdateLastLoginAsync(string adminId)
        {
            try
            {
                var adminRef = _firestore.Collection("admins").Document(adminId);
                await adminRef.UpdateAsync("LastLoginAt", DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error updating last login: {ex.Message}");
            }
        }
    }
}
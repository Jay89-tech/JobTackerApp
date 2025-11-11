using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace VisitorManagement.Models
{
    [FirestoreData]
    public class Admin
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string Email { get; set; }

        [FirestoreProperty]
        public string FullName { get; set; }

        [FirestoreProperty]
        public string Role { get; set; } // admin, superadmin

        [FirestoreProperty]
        public string Department { get; set; }

        [FirestoreProperty]
        public bool IsActive { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public DateTime LastLoginAt { get; set; }
    }
}

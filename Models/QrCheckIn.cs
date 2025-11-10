using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace VisitorManagement.Models
{
    [FirestoreData]
    public class QrCheckIn
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string VisitId { get; set; }

        [FirestoreProperty]
        public string VisitorId { get; set; }

        [FirestoreProperty]
        public DateTime CheckInTime { get; set; }

        [FirestoreProperty]
        public DateTime? CheckOutTime { get; set; }

        [FirestoreProperty]
        public string CheckInLocation { get; set; }

        [FirestoreProperty]
        public string CheckOutLocation { get; set; }

        [FirestoreProperty]
        public string VerifiedBy { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }
    }
}

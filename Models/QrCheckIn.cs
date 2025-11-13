using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace VisitorManagement.Models
{
    [FirestoreData]
    public class QrCheckIn
    {
        public string Id { get; set; }

        [FirestoreProperty("visitId")]
        public string VisitId { get; set; }

        [FirestoreProperty("visitorId")]
        public string VisitorId { get; set; }

        [FirestoreProperty("checkInTime")]
        public DateTime CheckInTime { get; set; }

        [FirestoreProperty("checkOutTime")]
        public DateTime? CheckOutTime { get; set; }

        [FirestoreProperty("checkInLocation")]
        public string CheckInLocation { get; set; }

        [FirestoreProperty("checkOutLocation")]
        public string? CheckOutLocation { get; set; }

        [FirestoreProperty("verifiedBy")]
        public string VerifiedBy { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }
}

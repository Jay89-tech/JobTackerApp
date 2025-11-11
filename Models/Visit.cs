using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace VisitorManagement.Models
{
    [FirestoreData]
    public class Visit
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string VisitorId { get; set; }

        [FirestoreProperty]
        public string VisitorName { get; set; }

        [FirestoreProperty]
        public string VisitorEmail { get; set; }

        [FirestoreProperty]
        public string VisitorPhone { get; set; }

        [FirestoreProperty]
        public string VisitorCompany { get; set; }

        [FirestoreProperty]
        public string PurposeOfVisit { get; set; }

        [FirestoreProperty]
        public string HostName { get; set; }

        [FirestoreProperty]
        public string HostDepartment { get; set; }

        [FirestoreProperty]
        public DateTime VisitDate { get; set; }

        [FirestoreProperty]
        public DateTime ExpectedArrivalTime { get; set; }

        [FirestoreProperty]
        public DateTime ExpectedDepartureTime { get; set; }

        [FirestoreProperty]
        public string Status { get; set; } // pending, approved, denied

        [FirestoreProperty]
        public string QrCode { get; set; }

        [FirestoreProperty]
        public string Notes { get; set; }

        [FirestoreProperty]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty]
        public DateTime UpdatedAt { get; set; }

        [FirestoreProperty]
        public string ApprovedBy { get; set; }

        [FirestoreProperty]
        public DateTime? ApprovedAt { get; set; }

        [FirestoreProperty]
        public string DenialReason { get; set; }
    }
}

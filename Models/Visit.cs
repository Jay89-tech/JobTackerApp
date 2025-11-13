using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace VisitorManagement.Models
{
    [FirestoreData]
    public class Visit
    {
        [FirestoreProperty("id")]
        public string Id { get; set; }

        [FirestoreProperty("visitorId")]
        public string VisitorId { get; set; }

        [FirestoreProperty("visitorName")]
        public string VisitorName { get; set; }

        [FirestoreProperty("visitorEmail")]
        public string VisitorEmail { get; set; }

        [FirestoreProperty("visitorPhone")]
        public string VisitorPhone { get; set; }

        [FirestoreProperty("visitorCompany")]
        public string VisitorCompany { get; set; }

        [FirestoreProperty("purposeOfVisit")]
        public string PurposeOfVisit { get; set; }

        [FirestoreProperty("hostName")]
        public string HostName { get; set; }

        [FirestoreProperty("hostDepartment")]
        public string HostDepartment { get; set; }

        [FirestoreProperty("visitDate")]
        public DateTime VisitDate { get; set; }

        [FirestoreProperty("expectedArrivalTime")]
        public DateTime ExpectedArrivalTime { get; set; }

        [FirestoreProperty("expectedDepatureTime")]
        public DateTime ExpectedDepartureTime { get; set; }

        [FirestoreProperty("status")]
        public string Status { get; set; } // pending, approved, denied

        [FirestoreProperty("qrCode")]
        public string QrCode { get; set; }

        [FirestoreProperty("notes")]
        public string Notes { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }

        [FirestoreProperty("updatedAt")]
        public DateTime UpdatedAt { get; set; }

        [FirestoreProperty("approveBy")]
        public string ApprovedBy { get; set; }

        [FirestoreProperty("approvedAt")]
        public DateTime? ApprovedAt { get; set; }

        [FirestoreProperty("denialReason")]
        public string DenialReason { get; set; }

        // Computed properties
        public bool IsApproved => Status?.ToLower() == "approved";
        public bool IsPending => Status?.ToLower() == "pending";
        public bool IsDenied => Status?.ToLower() == "denied";
    }
}

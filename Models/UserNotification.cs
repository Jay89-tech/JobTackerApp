using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace VisitorManagement.Models
{
    [FirestoreData]
    public class UserNotification
    {
        [FirestoreProperty("id")]
        public string Id { get; set; }

        [FirestoreProperty("userId")]
        public string UserId { get; set; }

        [FirestoreProperty("title")]
        public string Title { get; set; }

        [FirestoreProperty("message")]
        public string Message { get; set; }

        [FirestoreProperty("type")]
        public string Type { get; set; }

        [FirestoreProperty("relatedVisitId")]
        public string RelatedVisitId { get; set; }

        [FirestoreProperty("isRead")]
        public bool IsRead { get; set; }

        [FirestoreProperty("createdAt")]
        public DateTime CreatedAt { get; set; }
    }

}

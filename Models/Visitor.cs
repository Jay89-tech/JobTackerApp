using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace VisitorManagement.Models
{
    [FirestoreData]
    public class Visitor
    {
            public string Id { get; set; }

            [FirestoreProperty("email")]
            public string Email { get; set; }

            [FirestoreProperty("fullName")]
            public string FullName { get; set; }

            [FirestoreProperty("phone")]
            public string Phone { get; set; }

            [FirestoreProperty("company")]
            public string Company { get; set; }

            [FirestoreProperty("photoUrl")]
            public string? PhotoUrl { get; set; }

            [FirestoreProperty("fcmToken")]
            public string? FcmToken { get; set; }

            [FirestoreProperty("createdAt")]
            public DateTime CreatedAt { get; set; }

            [FirestoreProperty("updatedAt")]
            public DateTime UpdatedAt { get; set; }
        }
    }
using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace VisitorManagement.Models
{
    // View Models
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalVisitsToday { get; set; }
        public int PendingVisits { get; set; }
        public int ApprovedVisits { get; set; }
        public int CheckedInVisitors { get; set; }
        public List<Visit> RecentVisits { get; set; }
        public List<QrCheckIn> RecentCheckIns { get; set; }
    }
}

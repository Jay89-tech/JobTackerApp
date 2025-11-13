using System.ComponentModel.DataAnnotations;
using Google.Cloud.Firestore;

namespace SkillsManagement.Models
{
    [FirestoreData]
    public class Employee
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [Required]
        [FirestoreProperty]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [FirestoreProperty]
        public string Email { get; set; }

        [Required]
        [FirestoreProperty]
        public string Department { get; set; }

        [FirestoreProperty]
        public string Role { get; set; }

        [FirestoreProperty]
        public string ContactNumber { get; set; }

        [FirestoreProperty]
        public bool IsActive { get; set; } = true;

        [FirestoreProperty]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [FirestoreProperty]
        public DateTime? LastLogin { get; set; }

        [FirestoreProperty]
        public string PasswordHash { get; set; }

        [FirestoreProperty]
        public bool IsAdmin { get; set; } = false;
    }

    [FirestoreData]
    public class Qualification
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [Required]
        [FirestoreProperty]
        public string EmployeeId { get; set; }

        [Required]
        [FirestoreProperty]
        public string Title { get; set; }

        [Required]
        [FirestoreProperty]
        public string Institution { get; set; }

        [Required]
        [FirestoreProperty]
        public DateTime CompletionDate { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    [FirestoreData]
    public class Skill
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [Required]
        [FirestoreProperty]
        public string EmployeeId { get; set; }

        [Required]
        [FirestoreProperty]
        public string SkillName { get; set; }

        [Required]
        [FirestoreProperty]
        public string Category { get; set; } // Technical or Soft

        [Required]
        [Range(1, 5)]
        [FirestoreProperty]
        public int ProficiencyLevel { get; set; } // 1-5

        [FirestoreProperty]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        [FirestoreProperty]
        public DateTime? LastUpdated { get; set; }
    }

    [FirestoreData]
    public class Training
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [Required]
        [FirestoreProperty]
        public string EmployeeId { get; set; }

        [Required]
        [FirestoreProperty]
        public string TrainingName { get; set; }

        [FirestoreProperty]
        public string Provider { get; set; }

        [Required]
        [FirestoreProperty]
        public string Status { get; set; } // Completed, Planned, In Progress

        [FirestoreProperty]
        public DateTime? StartDate { get; set; }

        [FirestoreProperty]
        public DateTime? CompletionDate { get; set; }

        [FirestoreProperty]
        public string Description { get; set; }

        [FirestoreProperty]
        public bool IsSuggestedByHR { get; set; } = false;

        [FirestoreProperty]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    [FirestoreData]
    public class AuditLog
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string EmployeeId { get; set; }

        [FirestoreProperty]
        public string EmployeeName { get; set; }

        [FirestoreProperty]
        public string Action { get; set; }

        [FirestoreProperty]
        public string Module { get; set; } // Qualifications, Skills, Training, Profile

        [FirestoreProperty]
        public string Details { get; set; }

        [FirestoreProperty]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    [FirestoreData]
    public class Notification
    {
        [FirestoreProperty]
        public string Id { get; set; }

        [FirestoreProperty]
        public string EmployeeId { get; set; }

        [FirestoreProperty]
        public string Title { get; set; }

        [FirestoreProperty]
        public string Message { get; set; }

        [FirestoreProperty]
        public string Type { get; set; } // Suggestion, Update, Alert

        [FirestoreProperty]
        public bool IsRead { get; set; } = false;

        [FirestoreProperty]
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    }

    public class LoginViewModel
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class DashboardViewModel
    {
        public Employee CurrentUser { get; set; }
        public int TotalEmployees { get; set; }
        public int ActiveEmployees { get; set; }
        public int InactiveEmployees { get; set; }
        public int TotalSkills { get; set; }
        public int TotalQualifications { get; set; }
        public int TotalTrainings { get; set; }
        public int CompletedTrainings { get; set; }
        public int PlannedTrainings { get; set; }
        public List<AuditLog> RecentActivities { get; set; }
        public Dictionary<string, int> SkillDistribution { get; set; }
        public Dictionary<string, int> DepartmentDistribution { get; set; }
    }
}
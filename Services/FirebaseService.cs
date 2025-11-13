using Google.Cloud.Firestore;
using SkillsManagement.Models;
using System.Security.Cryptography;
using System.Text;

namespace SkillsManagement.Services
{
    public class FirebaseService
    {
        private readonly FirestoreDb _db;

        public FirebaseService()
        {
            // Use environment variable if set, otherwise use default
            var projectId = Environment.GetEnvironmentVariable("FIREBASE_PROJECT_ID") ?? "system-management-991ba";
            _db = FirestoreDb.Create(projectId);
        }

        // Employee Methods
        public async Task<Employee> GetEmployeeByEmailAsync(string email)
        {
            var query = _db.Collection("employees").WhereEqualTo("Email", email).Limit(1);
            var snapshot = await query.GetSnapshotAsync();

            if (snapshot.Documents.Count == 0)
                return null;

            var doc = snapshot.Documents[0];
            var employee = doc.ConvertTo<Employee>();
            employee.Id = doc.Id;
            return employee;
        }

        public async Task<Employee> GetEmployeeByIdAsync(string id)
        {
            var docRef = _db.Collection("employees").Document(id);
            var snapshot = await docRef.GetSnapshotAsync();

            if (!snapshot.Exists)
                return null;

            var employee = snapshot.ConvertTo<Employee>();
            employee.Id = snapshot.Id;
            return employee;
        }

        public async Task<List<Employee>> GetAllEmployeesAsync()
        {
            var snapshot = await _db.Collection("employees").GetSnapshotAsync();
            var employees = new List<Employee>();

            foreach (var doc in snapshot.Documents)
            {
                var employee = doc.ConvertTo<Employee>();
                employee.Id = doc.Id;
                employees.Add(employee);
            }

            return employees;
        }

        // Add this updated method to your FirebaseService.cs

        public async Task<string> CreateEmployeeAsync(Employee employee)
        {
            try
            {
                // Ensure Id is null so Firebase generates it
                employee.Id = null;

                // Set default values if not set
                if (string.IsNullOrEmpty(employee.PasswordHash))
                {
                    employee.PasswordHash = HashPassword("password123");
                }

                if (employee.CreatedDate == default(DateTime))
                {
                    employee.CreatedDate = DateTime.UtcNow;
                }

                // Add the employee document to Firestore
                var docRef = await _db.Collection("employees").AddAsync(employee);

                // Return the generated document ID
                return docRef.Id;
            }
            catch (Exception ex)
            {
                // Log the error with more details
                Console.WriteLine($"Error creating employee: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to create employee in Firebase: {ex.Message}", ex);
            }
        }

        // Replace the UpdateEmployeeAsync method in your FirebaseService.cs with this enhanced version

        public async Task UpdateEmployeeAsync(string id, Employee employee)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Employee ID cannot be null or empty", nameof(id));
                }

                if (employee == null)
                {
                    throw new ArgumentNullException(nameof(employee), "Employee object cannot be null");
                }

                // Get the document reference
                var docRef = _db.Collection("employees").Document(id);

                // Check if document exists
                var snapshot = await docRef.GetSnapshotAsync();
                if (!snapshot.Exists)
                {
                    throw new Exception($"Employee with ID {id} not found");
                }

                // Get the existing employee to preserve certain fields
                var existingEmployee = snapshot.ConvertTo<Employee>();

                // Preserve fields that shouldn't be changed during update
                employee.Id = id; // Ensure ID stays the same
                employee.CreatedDate = existingEmployee.CreatedDate; // Preserve creation date

                // If password hash is empty or null, preserve the existing one
                if (string.IsNullOrEmpty(employee.PasswordHash))
                {
                    employee.PasswordHash = existingEmployee.PasswordHash;
                }

                // Preserve LastLogin unless explicitly set
                if (!employee.LastLogin.HasValue && existingEmployee.LastLogin.HasValue)
                {
                    employee.LastLogin = existingEmployee.LastLogin;
                }

                // Create update dictionary with all fields
                var updates = new Dictionary<string, object>
        {
            { "FullName", employee.FullName ?? "" },
            { "Email", employee.Email ?? "" },
            { "Department", employee.Department ?? "" },
            { "Role", employee.Role ?? "" },
            { "ContactNumber", employee.ContactNumber ?? "" },
            { "IsActive", employee.IsActive },
            { "IsAdmin", employee.IsAdmin },
            { "PasswordHash", employee.PasswordHash },
            { "CreatedDate", employee.CreatedDate },
            { "LastLogin", employee.LastLogin }
        };

                // Update the document
                await docRef.UpdateAsync(updates);

                Console.WriteLine($"Successfully updated employee: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating employee {id}: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to update employee in Firebase: {ex.Message}", ex);
            }
        }

        // Alternative method using SetAsync with merge (simpler approach)
        public async Task UpdateEmployeeAsyncV2(string id, Employee employee)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Employee ID cannot be null or empty", nameof(id));
                }

                if (employee == null)
                {
                    throw new ArgumentNullException(nameof(employee), "Employee object cannot be null");
                }

                // Ensure the ID matches
                employee.Id = id;

                // Get the document reference
                var docRef = _db.Collection("employees").Document(id);

                // Use SetAsync with MergeAll to update only provided fields
                await docRef.SetAsync(employee, SetOptions.MergeAll);

                Console.WriteLine($"Successfully updated employee: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating employee {id}: {ex.Message}");
                throw new Exception($"Failed to update employee in Firebase: {ex.Message}", ex);
            }
        }

        // Additional helper method for updating specific fields
        public async Task UpdateEmployeeFieldsAsync(string id, Dictionary<string, object> fields)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    throw new ArgumentException("Employee ID cannot be null or empty", nameof(id));
                }

                if (fields == null || fields.Count == 0)
                {
                    throw new ArgumentException("Fields dictionary cannot be null or empty", nameof(fields));
                }

                var docRef = _db.Collection("employees").Document(id);
                await docRef.UpdateAsync(fields);

                Console.WriteLine($"Successfully updated {fields.Count} field(s) for employee: {id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating employee fields {id}: {ex.Message}");
                throw new Exception($"Failed to update employee fields in Firebase: {ex.Message}", ex);
            }
        }

        public async Task UpdateEmployeeStatusAsync(string id, bool isActive)
        {
            var docRef = _db.Collection("employees").Document(id);
            await docRef.UpdateAsync("IsActive", isActive);
        }

        public async Task UpdateLastLoginAsync(string id)
        {
            var docRef = _db.Collection("employees").Document(id);
            await docRef.UpdateAsync("LastLogin", DateTime.UtcNow);
        }

        // Qualification Methods
        public async Task<List<Qualification>> GetQualificationsByEmployeeAsync(string employeeId)
        {
            var query = _db.Collection("qualifications").WhereEqualTo("EmployeeId", employeeId);
            var snapshot = await query.GetSnapshotAsync();
            var qualifications = new List<Qualification>();

            foreach (var doc in snapshot.Documents)
            {
                var qual = doc.ConvertTo<Qualification>();
                qual.Id = doc.Id;
                qualifications.Add(qual);
            }

            return qualifications;
        }

        public async Task<int> GetTotalQualificationsCountAsync()
        {
            var snapshot = await _db.Collection("qualifications").GetSnapshotAsync();
            return snapshot.Count;
        }

        // Skill Methods
        public async Task<List<Skill>> GetSkillsByEmployeeAsync(string employeeId)
        {
            var query = _db.Collection("skills").WhereEqualTo("EmployeeId", employeeId);
            var snapshot = await query.GetSnapshotAsync();
            var skills = new List<Skill>();

            foreach (var doc in snapshot.Documents)
            {
                var skill = doc.ConvertTo<Skill>();
                skill.Id = doc.Id;
                skills.Add(skill);
            }

            return skills;
        }

        public async Task<int> GetTotalSkillsCountAsync()
        {
            var snapshot = await _db.Collection("skills").GetSnapshotAsync();
            return snapshot.Count;
        }

        public async Task<Dictionary<string, int>> GetSkillDistributionAsync()
        {
            var snapshot = await _db.Collection("skills").GetSnapshotAsync();
            var distribution = new Dictionary<string, int>();

            foreach (var doc in snapshot.Documents)
            {
                var skill = doc.ConvertTo<Skill>();
                var category = skill.Category ?? "Other";

                if (distribution.ContainsKey(category))
                    distribution[category]++;
                else
                    distribution[category] = 1;
            }

            return distribution;
        }


        // Add this method to your FirebaseService.cs class

        public async Task DeleteEmployeeAsync(string id)
        {
            // Delete employee document
            var docRef = _db.Collection("employees").Document(id);
            await docRef.DeleteAsync();

            // Also delete all associated data
            // Delete skills
            var skillsQuery = _db.Collection("skills").WhereEqualTo("EmployeeId", id);
            var skillsSnapshot = await skillsQuery.GetSnapshotAsync();
            foreach (var skillDoc in skillsSnapshot.Documents)
            {
                await skillDoc.Reference.DeleteAsync();
            }

            // Delete qualifications
            var qualsQuery = _db.Collection("qualifications").WhereEqualTo("EmployeeId", id);
            var qualsSnapshot = await qualsQuery.GetSnapshotAsync();
            foreach (var qualDoc in qualsSnapshot.Documents)
            {
                await qualDoc.Reference.DeleteAsync();
            }

            // Delete trainings
            var trainingsQuery = _db.Collection("trainings").WhereEqualTo("EmployeeId", id);
            var trainingsSnapshot = await trainingsQuery.GetSnapshotAsync();
            foreach (var trainingDoc in trainingsSnapshot.Documents)
            {
                await trainingDoc.Reference.DeleteAsync();
            }

            // Delete notifications
            var notificationsQuery = _db.Collection("notifications").WhereEqualTo("EmployeeId", id);
            var notificationsSnapshot = await notificationsQuery.GetSnapshotAsync();
            foreach (var notificationDoc in notificationsSnapshot.Documents)
            {
                await notificationDoc.Reference.DeleteAsync();
            }
        }

        // Training Methods
        public async Task<List<Training>> GetTrainingsByEmployeeAsync(string employeeId)
        {
            var query = _db.Collection("trainings").WhereEqualTo("EmployeeId", employeeId);
            var snapshot = await query.GetSnapshotAsync();
            var trainings = new List<Training>();

            foreach (var doc in snapshot.Documents)
            {
                var training = doc.ConvertTo<Training>();
                training.Id = doc.Id;
                trainings.Add(training);
            }

            return trainings;
        }

        public async Task<int> GetTotalTrainingsCountAsync()
        {
            var snapshot = await _db.Collection("trainings").GetSnapshotAsync();
            return snapshot.Count;
        }

        public async Task<int> GetCompletedTrainingsCountAsync()
        {
            var query = _db.Collection("trainings").WhereEqualTo("Status", "Completed");
            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Count;
        }

        public async Task<int> GetPlannedTrainingsCountAsync()
        {
            var query = _db.Collection("trainings").WhereEqualTo("Status", "Planned");
            var snapshot = await query.GetSnapshotAsync();
            return snapshot.Count;
        }

        public async Task<string> CreateTrainingSuggestionAsync(Training training)
        {
            training.IsSuggestedByHR = true;
            training.CreatedDate = DateTime.UtcNow;
            training.Id = null; // Let Firestore generate the ID

            var docRef = await _db.Collection("trainings").AddAsync(training);
            return docRef.Id;
        }

        // Audit Log Methods
        public async Task CreateAuditLogAsync(AuditLog log)
        {
            log.Timestamp = DateTime.UtcNow;
            log.Id = null; // Let Firestore generate the ID
            await _db.Collection("auditLogs").AddAsync(log);
        }

        public async Task<List<AuditLog>> GetRecentAuditLogsAsync(int limit = 10)
        {
            var query = _db.Collection("auditLogs")
                .OrderByDescending("Timestamp")
                .Limit(limit);

            var snapshot = await query.GetSnapshotAsync();
            var logs = new List<AuditLog>();

            foreach (var doc in snapshot.Documents)
            {
                var log = doc.ConvertTo<AuditLog>();
                log.Id = doc.Id;
                logs.Add(log);
            }

            return logs;
        }



        // Add these methods to your existing FirebaseService.cs class

// Qualification Methods (Add to existing FirebaseService)

public async Task<Qualification> GetQualificationByIdAsync(string id)
{
    var docRef = _db.Collection("qualifications").Document(id);
    var snapshot = await docRef.GetSnapshotAsync();

    if (!snapshot.Exists)
        return null;

    var qualification = snapshot.ConvertTo<Qualification>();
    qualification.Id = snapshot.Id;
    return qualification;
}

public async Task<List<Qualification>> GetAllQualificationsAsync()
{
    var snapshot = await _db.Collection("qualifications").GetSnapshotAsync();
    var qualifications = new List<Qualification>();

    foreach (var doc in snapshot.Documents)
    {
        var qual = doc.ConvertTo<Qualification>();
        qual.Id = doc.Id;
        qualifications.Add(qual);
    }

    return qualifications;
}

public async Task<string> CreateQualificationAsync(Qualification qualification)
{
    qualification.CreatedDate = DateTime.UtcNow;
    qualification.Id = null; // Let Firestore generate the ID
    var docRef = await _db.Collection("qualifications").AddAsync(qualification);
    return docRef.Id;
}

public async Task UpdateQualificationAsync(string id, Qualification qualification)
{
    var docRef = _db.Collection("qualifications").Document(id);
    await docRef.SetAsync(qualification, SetOptions.MergeAll);
}

public async Task DeleteQualificationAsync(string id)
{
    var docRef = _db.Collection("qualifications").Document(id);
    await docRef.DeleteAsync();
}

public async Task<Dictionary<string, int>> GetQualificationDistributionAsync()
{
    var snapshot = await _db.Collection("qualifications").GetSnapshotAsync();
    var distribution = new Dictionary<string, int>();

    foreach (var doc in snapshot.Documents)
    {
        var qual = doc.ConvertTo<Qualification>();
        var institution = qual.Institution ?? "Other";

        if (distribution.ContainsKey(institution))
            distribution[institution]++;
        else
            distribution[institution] = 1;
    }

    return distribution;
}

public async Task<int> GetQualificationsCountForEmployeeAsync(string employeeId)
{
    var query = _db.Collection("qualifications").WhereEqualTo("EmployeeId", employeeId);
    var snapshot = await query.GetSnapshotAsync();
    return snapshot.Count;
}

        // Notification Methods
        public async Task<string> CreateNotificationAsync(Notification notification)
        {
            notification.CreatedDate = DateTime.UtcNow;
            notification.Id = null; // Let Firestore generate the ID
            var docRef = await _db.Collection("notifications").AddAsync(notification);
            return docRef.Id;
        }




        // Helper Methods
        public string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }

        public bool VerifyPassword(string password, string hash)
        {
            string hashOfInput = HashPassword(password);
            return StringComparer.OrdinalIgnoreCase.Compare(hashOfInput, hash) == 0;
        }

        public async Task<Dictionary<string, int>> GetDepartmentDistributionAsync()
        {
            var snapshot = await _db.Collection("employees").GetSnapshotAsync();
            var distribution = new Dictionary<string, int>();

            foreach (var doc in snapshot.Documents)
            {
                var employee = doc.ConvertTo<Employee>();
                var dept = employee.Department ?? "Unassigned";

                if (distribution.ContainsKey(dept))
                    distribution[dept]++;
                else
                    distribution[dept] = 1;
            }

            return distribution;
        }
    }
}
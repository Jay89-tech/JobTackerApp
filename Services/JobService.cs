using JobTrackerApp.Data;
using JobTrackerApp.Models;
using Microsoft.EntityFrameworkCore;

namespace JobTrackerApp.Services
{
    public interface IJobService
    {
        Task<IEnumerable<Job>> GetAllJobsAsync();
        Task<Job?> GetJobByIdAsync(int id);
        Task<Job> CreateJobAsync(Job job);
        Task<Job> UpdateJobAsync(Job job);
        Task<bool> DeleteJobAsync(int id);
        Task<IEnumerable<Job>> SearchJobsAsync(string searchTerm);
        Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status);
        Task<IEnumerable<Job>> GetRecentJobsAsync(int days = 7);
        Task<IEnumerable<Job>> GetJobsByCompanyAsync(string company);
        Task<JobStatistics> GetJobStatisticsAsync();
    }

    public class JobService : IJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<JobService> _logger;

        public JobService(ApplicationDbContext context, ILogger<JobService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Job>> GetAllJobsAsync()
        {
            try
            {
                return await _context.Jobs
                    .Include(j => j.Applications)
                    .OrderByDescending(j => j.DatePosted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all jobs");
                throw;
            }
        }

        public async Task<Job?> GetJobByIdAsync(int id)
        {
            try
            {
                return await _context.Jobs
                    .Include(j => j.Applications)
                    .ThenInclude(a => a.User)
                    .FirstOrDefaultAsync(j => j.Id == id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job with ID: {JobId}", id);
                throw;
            }
        }

        public async Task<Job> CreateJobAsync(Job job)
        {
            try
            {
                job.CreatedAt = DateTime.UtcNow;
                job.UpdatedAt = DateTime.UtcNow;

                _context.Jobs.Add(job);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Job created successfully: {JobTitle} at {Company}", job.Title, job.Company);
                return job;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating job: {JobTitle}", job.Title);
                throw;
            }
        }

        public async Task<Job> UpdateJobAsync(Job job)
        {
            try
            {
                var existingJob = await _context.Jobs.FindAsync(job.Id);
                if (existingJob == null)
                {
                    throw new InvalidOperationException($"Job with ID {job.Id} not found");
                }

                // Update properties
                existingJob.Title = job.Title;
                existingJob.Company = job.Company;
                existingJob.Description = job.Description;
                existingJob.Location = job.Location;
                existingJob.Salary = job.Salary;
                existingJob.Status = job.Status;
                existingJob.ApplicationDeadline = job.ApplicationDeadline;
                existingJob.Requirements = job.Requirements;
                existingJob.JobType = job.JobType;
                existingJob.ExperienceLevel = job.ExperienceLevel;
                existingJob.IsRemote = job.IsRemote;
                existingJob.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Job updated successfully: {JobTitle} at {Company}", job.Title, job.Company);
                return existingJob;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating job with ID: {JobId}", job.Id);
                throw;
            }
        }

        public async Task<bool> DeleteJobAsync(int id)
        {
            try
            {
                var job = await _context.Jobs.FindAsync(id);
                if (job == null)
                {
                    return false;
                }

                _context.Jobs.Remove(job);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Job deleted successfully: ID {JobId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting job with ID: {JobId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Job>> SearchJobsAsync(string searchTerm)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(searchTerm))
                {
                    return await GetAllJobsAsync();
                }

                var lowercaseSearchTerm = searchTerm.ToLower();

                return await _context.Jobs
                    .Include(j => j.Applications)
                    .Where(j => j.Title.ToLower().Contains(lowercaseSearchTerm) ||
                               j.Company.ToLower().Contains(lowercaseSearchTerm) ||
                               j.Description.ToLower().Contains(lowercaseSearchTerm) ||
                               j.Location.ToLower().Contains(lowercaseSearchTerm) ||
                               j.Requirements.ToLower().Contains(lowercaseSearchTerm))
                    .OrderByDescending(j => j.DatePosted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching jobs with term: {SearchTerm}", searchTerm);
                throw;
            }
        }

        public async Task<IEnumerable<Job>> GetJobsByStatusAsync(JobStatus status)
        {
            try
            {
                return await _context.Jobs
                    .Include(j => j.Applications)
                    .Where(j => j.Status == status)
                    .OrderByDescending(j => j.DatePosted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs by status: {Status}", status);
                throw;
            }
        }

        public async Task<IEnumerable<Job>> GetRecentJobsAsync(int days = 7)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-days);

                return await _context.Jobs
                    .Include(j => j.Applications)
                    .Where(j => j.DatePosted >= cutoffDate)
                    .OrderByDescending(j => j.DatePosted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving recent jobs for {Days} days", days);
                throw;
            }
        }

        public async Task<IEnumerable<Job>> GetJobsByCompanyAsync(string company)
        {
            try
            {
                return await _context.Jobs
                    .Include(j => j.Applications)
                    .Where(j => j.Company.ToLower().Contains(company.ToLower()))
                    .OrderByDescending(j => j.DatePosted)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving jobs for company: {Company}", company);
                throw;
            }
        }

        public async Task<JobStatistics> GetJobStatisticsAsync()
        {
            try
            {
                var totalJobs = await _context.Jobs.CountAsync();
                var openJobs = await _context.Jobs.CountAsync(j => j.Status == JobStatus.Open);
                var closedJobs = await _context.Jobs.CountAsync(j => j.Status == JobStatus.Closed);
                var filledJobs = await _context.Jobs.CountAsync(j => j.Status == JobStatus.Filled);
                var totalApplications = await _context.Applications.CountAsync();
                var recentJobs = await _context.Jobs.CountAsync(j => j.DatePosted >= DateTime.UtcNow.AddDays(-7));

                var topCompanies = await _context.Jobs
                    .GroupBy(j => j.Company)
                    .Select(g => new { Company = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToDictionaryAsync(x => x.Company, x => x.Count);

                var averageSalary = await _context.Jobs
                    .Where(j => j.Salary.HasValue)
                    .AverageAsync(j => j.Salary.Value);

                return new JobStatistics
                {
                    TotalJobs = totalJobs,
                    OpenJobs = openJobs,
                    ClosedJobs = closedJobs,
                    FilledJobs = filledJobs,
                    TotalApplications = totalApplications,
                    RecentJobs = recentJobs,
                    TopCompanies = topCompanies,
                    AverageSalary = averageSalary
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving job statistics");
                throw;
            }
        }
    }

    public class JobStatistics
    {
        public int TotalJobs { get; set; }
        public int OpenJobs { get; set; }
        public int ClosedJobs { get; set; }
        public int FilledJobs { get; set; }
        public int TotalApplications { get; set; }
        public int RecentJobs { get; set; }
        public Dictionary<string, int> TopCompanies { get; set; } = new();
        public decimal AverageSalary { get; set; }
    }
}
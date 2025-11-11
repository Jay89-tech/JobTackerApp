using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Models;
using VisitorManagement.Services;

namespace VisitorManagement.Controllers
{
    [Authorize]
    public class ReportsController : Controller
    {
        private readonly IVisitService _visitService;
        private readonly ICheckInService _checkInService;

        public ReportsController(IVisitService visitService, ICheckInService checkInService)
        {
            _visitService = visitService;
            _checkInService = checkInService;
        }

        // GET: Reports
        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new ReportsViewModel
                {
                    StartDate = DateTime.Today.AddDays(-30),
                    EndDate = DateTime.Today
                };

                await LoadReportDataAsync(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading reports: {ex.Message}";
                return View(new ReportsViewModel());
            }
        }

        // POST: Reports/Generate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Generate(DateTime startDate, DateTime endDate, string reportType)
        {
            try
            {
                var viewModel = new ReportsViewModel
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    ReportType = reportType
                };

                await LoadReportDataAsync(viewModel);
                return View("Index", viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error generating report: {ex.Message}";
                return View("Index", new ReportsViewModel());
            }
        }

        private async Task LoadReportDataAsync(ReportsViewModel viewModel)
        {
            var allVisits = await _visitService.GetAllVisitsAsync();
            var allCheckIns = await _checkInService.GetTodayCheckInsAsync();

            // Filter by date range
            var filteredVisits = allVisits
                .Where(v => v.VisitDate >= viewModel.StartDate && v.VisitDate <= viewModel.EndDate.AddDays(1))
                .ToList();

            // Calculate statistics
            viewModel.TotalVisits = filteredVisits.Count;
            viewModel.ApprovedVisits = filteredVisits.Count(v => v.Status == "approved");
            viewModel.PendingVisits = filteredVisits.Count(v => v.Status == "pending");
            viewModel.DeniedVisits = filteredVisits.Count(v => v.Status == "denied");
            viewModel.TotalCheckIns = allCheckIns.Count;

            // Group by date for chart
            viewModel.VisitsByDate = filteredVisits
                .GroupBy(v => v.VisitDate.Date)
                .Select(g => new DateCount
                {
                    Date = g.Key,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            // Group by status
            viewModel.VisitsByStatus = filteredVisits
                .GroupBy(v => v.Status)
                .Select(g => new StatusCount
                {
                    Status = g.Key,
                    Count = g.Count()
                })
                .ToList();

            // Top companies
            viewModel.TopCompanies = filteredVisits
                .GroupBy(v => v.VisitorCompany)
                .Select(g => new CompanyCount
                {
                    Company = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            // Top hosts
            viewModel.TopHosts = filteredVisits
                .GroupBy(v => v.HostName)
                .Select(g => new HostCount
                {
                    HostName = g.Key,
                    Count = g.Count()
                })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            // Average visits per day
            var days = (viewModel.EndDate - viewModel.StartDate).Days + 1;
            viewModel.AverageVisitsPerDay = days > 0 ? (double)viewModel.TotalVisits / days : 0;

            // Recent visits for detailed view
            viewModel.RecentVisits = filteredVisits
                .OrderByDescending(v => v.CreatedAt)
                .Take(50)
                .ToList();
        }

        // GET: Reports/Export
        public async Task<IActionResult> Export(DateTime startDate, DateTime endDate, string format = "csv")
        {
            try
            {
                var allVisits = await _visitService.GetAllVisitsAsync();
                var filteredVisits = allVisits
                    .Where(v => v.VisitDate >= startDate && v.VisitDate <= endDate.AddDays(1))
                    .OrderByDescending(v => v.VisitDate)
                    .ToList();

                if (format.ToLower() == "csv")
                {
                    var csv = GenerateCSV(filteredVisits);
                    var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
                    return File(bytes, "text/csv", $"visits_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.csv");
                }

                return BadRequest("Unsupported format");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error exporting report: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        private string GenerateCSV(List<Visit> visits)
        {
            var sb = new System.Text.StringBuilder();

            // Header
            sb.AppendLine("Visit Date,Visitor Name,Email,Company,Phone,Host Name,Department,Purpose,Status,Approved By,Approval Date");

            // Data
            foreach (var visit in visits)
            {
                sb.AppendLine($"\"{visit.VisitDate:yyyy-MM-dd}\",\"{visit.VisitorName}\",\"{visit.VisitorEmail}\",\"{visit.VisitorCompany}\",\"{visit.VisitorPhone}\",\"{visit.HostName}\",\"{visit.HostDepartment}\",\"{visit.PurposeOfVisit}\",\"{visit.Status}\",\"{visit.ApprovedBy ?? ""}\",\"{visit.ApprovedAt?.ToString("yyyy-MM-dd HH:mm") ?? ""}\"");
            }

            return sb.ToString();
        }
    }

    // View Models for Reports
    public class ReportsViewModel
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string ReportType { get; set; }

        // Statistics
        public int TotalVisits { get; set; }
        public int ApprovedVisits { get; set; }
        public int PendingVisits { get; set; }
        public int DeniedVisits { get; set; }
        public int TotalCheckIns { get; set; }
        public double AverageVisitsPerDay { get; set; }

        // Chart Data
        public List<DateCount> VisitsByDate { get; set; }
        public List<StatusCount> VisitsByStatus { get; set; }
        public List<CompanyCount> TopCompanies { get; set; }
        public List<HostCount> TopHosts { get; set; }

        // Detailed Data
        public List<Visit> RecentVisits { get; set; }
    }

    public class DateCount
    {
        public DateTime Date { get; set; }
        public int Count { get; set; }
    }

    public class StatusCount
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }

    public class CompanyCount
    {
        public string Company { get; set; }
        public int Count { get; set; }
    }

    public class HostCount
    {
        public string HostName { get; set; }
        public int Count { get; set; }
    }
}
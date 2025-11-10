using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Models;
using VisitorManagement.Services;

namespace VisitorManagement.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IVisitService _visitService;
        private readonly ICheckInService _checkInService;

        public DashboardController(IVisitService visitService, ICheckInService checkInService)
        {
            _visitService = visitService;
            _checkInService = checkInService;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var viewModel = new DashboardViewModel
                {
                    TotalVisitsToday = await _visitService.GetTotalVisitsTodayAsync(),
                    PendingVisits = await _visitService.GetPendingVisitsCountAsync(),
                    ApprovedVisits = await _visitService.GetApprovedVisitsCountAsync(),
                    CheckedInVisitors = await _checkInService.GetCheckedInCountAsync(),
                    RecentVisits = await _visitService.GetTodayVisitsAsync(),
                    RecentCheckIns = await _checkInService.GetRecentCheckInsAsync(5)
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading dashboard: {ex.Message}";
                return View(new DashboardViewModel());
            }
        }
    }
}
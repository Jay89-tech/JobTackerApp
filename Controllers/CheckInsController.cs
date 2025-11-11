using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Models;
using VisitorManagement.Services;

namespace VisitorManagement.Controllers
{
    [Authorize]
    public class CheckInsController : Controller
    {
        private readonly ICheckInService _checkInService;
        private readonly IVisitService _visitService;
        private readonly IVisitorService _visitorService;

        public CheckInsController(
            ICheckInService checkInService,
            IVisitService visitService,
            IVisitorService visitorService)
        {
            _checkInService = checkInService;
            _visitService = visitService;
            _visitorService = visitorService;
        }

        // GET: CheckIns
        public async Task<IActionResult> Index(DateTime? date = null)
        {
            try
            {
                List<QrCheckIn> checkIns;

                if (date.HasValue)
                {
                    // Filter by specific date - would need additional service method
                    checkIns = await _checkInService.GetTodayCheckInsAsync();
                }
                else
                {
                    checkIns = await _checkInService.GetTodayCheckInsAsync();
                }

                // Enrich check-in data with visit and visitor information
                var enrichedCheckIns = new List<CheckInViewModel>();

                foreach (var checkIn in checkIns)
                {
                    var visit = await _visitService.GetVisitByIdAsync(checkIn.VisitId);
                    Visitor visitor = null;

                    if (visit != null)
                    {
                        visitor = await _visitorService.GetVisitorByIdAsync(visit.VisitorId);
                    }

                    enrichedCheckIns.Add(new CheckInViewModel
                    {
                        CheckIn = checkIn,
                        Visit = visit,
                        Visitor = visitor
                    });
                }

                ViewBag.SelectedDate = date ?? DateTime.Today;
                return View(enrichedCheckIns);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading check-ins: {ex.Message}";
                return View(new List<CheckInViewModel>());
            }
        }

        // GET: CheckIns/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var checkIns = await _checkInService.GetTodayCheckInsAsync();
                var checkIn = checkIns.FirstOrDefault(c => c.Id == id);

                if (checkIn == null)
                {
                    return NotFound();
                }

                var visit = await _visitService.GetVisitByIdAsync(checkIn.VisitId);
                Visitor visitor = null;

                if (visit != null)
                {
                    visitor = await _visitorService.GetVisitorByIdAsync(visit.VisitorId);
                }

                var viewModel = new CheckInViewModel
                {
                    CheckIn = checkIn,
                    Visit = visit,
                    Visitor = visitor
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading check-in details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: CheckIns/Active
        public async Task<IActionResult> Active()
        {
            try
            {
                var checkIns = await _checkInService.GetTodayCheckInsAsync();
                var activeCheckIns = checkIns.Where(c => !c.CheckOutTime.HasValue).ToList();

                var enrichedCheckIns = new List<CheckInViewModel>();

                foreach (var checkIn in activeCheckIns)
                {
                    var visit = await _visitService.GetVisitByIdAsync(checkIn.VisitId);
                    Visitor visitor = null;

                    if (visit != null)
                    {
                        visitor = await _visitorService.GetVisitorByIdAsync(visit.VisitorId);
                    }

                    enrichedCheckIns.Add(new CheckInViewModel
                    {
                        CheckIn = checkIn,
                        Visit = visit,
                        Visitor = visitor
                    });
                }

                ViewData["Title"] = "Active Check-Ins";
                return View("Index", enrichedCheckIns);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading active check-ins: {ex.Message}";
                return View("Index", new List<CheckInViewModel>());
            }
        }
    }

    // View Model for enriched check-in data
    public class CheckInViewModel
    {
        public QrCheckIn CheckIn { get; set; }
        public Visit Visit { get; set; }
        public Visitor Visitor { get; set; }
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VisitorManagement.Models;
using VisitorManagement.Services;

namespace VisitorManagement.Controllers
{
    [Authorize]
    public class VisitorsController : Controller
    {
        private readonly IVisitorService _visitorService;
        private readonly IVisitService _visitService;

        public VisitorsController(IVisitorService visitorService, IVisitService visitService)
        {
            _visitorService = visitorService;
            _visitService = visitService;
        }

        // GET: Visitors
        public async Task<IActionResult> Index()
        {
            try
            {
                var visitors = await _visitorService.GetAllVisitorsAsync();
                return View(visitors);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading visitors: {ex.Message}";
                return View(new List<Visitor>());
            }
        }

        // GET: Visitors/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var visitor = await _visitorService.GetVisitorByIdAsync(id);
                if (visitor == null)
                {
                    return NotFound();
                }

                // Get all visits for this visitor
                var allVisits = await _visitService.GetAllVisitsAsync();
                var visitorVisits = allVisits.Where(v => v.VisitorId == id)
                    .OrderByDescending(v => v.VisitDate)
                    .ToList();

                ViewBag.Visits = visitorVisits;
                return View(visitor);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading visitor details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Visitors/Search
        public async Task<IActionResult> Search(string searchTerm)
        {
            try
            {
                var visitors = await _visitorService.GetAllVisitorsAsync();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    visitors = visitors.Where(v =>
                        v.FullName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.Company.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                        v.Phone.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)
                    ).ToList();
                }

                ViewBag.SearchTerm = searchTerm;
                return View("Index", visitors);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error searching visitors: {ex.Message}";
                return View("Index", new List<Visitor>());
            }
        }
    }
}
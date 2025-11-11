using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using VisitorManagement.Models;
using VisitorManagement.Services;

namespace VisitorManagement.Controllers
{
    [Authorize]
    public class VisitsController : Controller
    {
        private readonly IVisitService _visitService;
        private readonly IVisitorService _visitorService;

        public VisitsController(IVisitService visitService, IVisitorService visitorService)
        {
            _visitService = visitService;
            _visitorService = visitorService;
        }

        // GET: Visits
        public async Task<IActionResult> Index(string status = null, DateTime? date = null)
        {
            try
            {
                List<Visit> visits;

                if (!string.IsNullOrEmpty(status))
                {
                    if (status.ToLower() == "pending")
                        visits = await _visitService.GetPendingVisitsAsync();
                    else
                        visits = await _visitService.GetAllVisitsAsync();
                }
                else if (date.HasValue)
                {
                    visits = await _visitService.GetTodayVisitsAsync();
                }
                else
                {
                    visits = await _visitService.GetAllVisitsAsync();
                }

                ViewBag.Status = status;
                ViewBag.Date = date;
                return View(visits);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading visits: {ex.Message}";
                return View(new List<Visit>());
            }
        }

        // GET: Visits/Pending
        public async Task<IActionResult> Pending()
        {
            try
            {
                var visits = await _visitService.GetPendingVisitsAsync();
                ViewData["Title"] = "Pending Visits";
                return View("Pending", visits);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading pending visits: {ex.Message}";
                return View("Pending", new List<Visit>());
            }
        }

        // GET: Visits/Details/5
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var visit = await _visitService.GetVisitByIdAsync(id);
                if (visit == null)
                {
                    return NotFound();
                }

                // Get visitor details
                var visitor = await _visitorService.GetVisitorByIdAsync(visit.VisitorId);
                ViewBag.Visitor = visitor;

                return View(visit);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading visit details: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // POST: Visits/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var success = await _visitService.ApproveVisitAsync(id, adminId);

                if (success)
                {
                    TempData["Success"] = "Visit approved successfully and visitor has been notified.";
                }
                else
                {
                    TempData["Error"] = "Failed to approve visit.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error approving visit: {ex.Message}";
            }

            return RedirectToAction(nameof(Pending));
        }

        // POST: Visits/Deny/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deny(string id, string reason)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            if (string.IsNullOrEmpty(reason))
            {
                TempData["Error"] = "Please provide a reason for denial.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                var adminId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var success = await _visitService.DenyVisitAsync(id, adminId, reason);

                if (success)
                {
                    TempData["Success"] = "Visit denied successfully and visitor has been notified.";
                }
                else
                {
                    TempData["Error"] = "Failed to deny visit.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error denying visit: {ex.Message}";
            }

            return RedirectToAction(nameof(Pending));
        }

        // GET: Visits/Search
        public async Task<IActionResult> Search(string searchTerm, string searchType)
        {
            try
            {
                List<Visit> visits;

                if (string.IsNullOrEmpty(searchTerm))
                {
                    visits = await _visitService.GetAllVisitsAsync();
                }
                else
                {
                    visits = await _visitService.GetAllVisitsAsync();

                    // Filter based on search type
                    visits = searchType switch
                    {
                        "name" => visits.Where(v => v.VisitorName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList(),
                        "email" => visits.Where(v => v.VisitorEmail.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList(),
                        "company" => visits.Where(v => v.VisitorCompany.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList(),
                        "host" => visits.Where(v => v.HostName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList(),
                        _ => visits
                    };
                }

                ViewBag.SearchTerm = searchTerm;
                ViewBag.SearchType = searchType;
                return View("Index", visits);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error searching visits: {ex.Message}";
                return View("Index", new List<Visit>());
            }
        }
    }
}
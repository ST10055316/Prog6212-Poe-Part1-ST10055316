using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http; // Add this
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services;
using Claim = ContractMonthlyClaimSystem.Models.Claim;

namespace ContractMonthlyClaimSystem.Controllers
{
    [Authorize]
    public class ClaimController : Controller
    {
        private readonly IClaimService _claimService;
        private readonly IUserService _userService;

        public ClaimController(IClaimService claimService, IUserService userService)
        {
            _claimService = claimService;
            _userService = userService;
        }

        // GET: Claims
        public async Task<IActionResult> Index(ClaimSearchViewModel searchModel)
        {
            var currentUser = await GetCurrentUserAsync();

            if (currentUser != null && currentUser.Role == UserRole.Lecturer)
            {
                searchModel.LecturerId = currentUser.UserId;
            }

            var claims = await _claimService.SearchClaimsAsync(searchModel, currentUser);
            searchModel.Results = claims.Claims;
            searchModel.TotalResults = claims.TotalCount;

            if (currentUser != null && currentUser.Role != UserRole.Lecturer)
            {
                searchModel.AvailableLecturers = await _userService.GetLecturersAsync();
            }

            return View(searchModel);
        }

        // GET: Claims/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var claim = await _claimService.GetClaimByIdAsync(id);

            if (claim == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this claim
            if (currentUser.Role == UserRole.Lecturer && claim.LecturerId != currentUser.UserId)
            {
                return Forbid();
            }

            var viewModel = MapClaimToViewModel(claim, currentUser);
            return View(viewModel);
        }

        // GET: Claims/Create
        [Authorize(Roles = "Lecturer")]
        public IActionResult Create()
        {
            var model = new SubmitClaimViewModel
            {
                ClaimPeriod = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1)
            };
            return View(model);
        }

        // POST: Claims/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Create(SubmitClaimViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await GetCurrentUserAsync();
                    if (currentUser == null) return Unauthorized();

                    var claim = new Claim
                    {
                        LecturerId = currentUser.UserId,
                        HoursWorked = model.HoursWorked,
                        HourlyRate = model.HourlyRate,
                        ClaimPeriod = model.ClaimPeriod,
                        Description = model.Description,
                        Status = ClaimStatus.Submitted
                    };

                    var claimId = await _claimService.SubmitClaimAsync(claim);

                    TempData["SuccessMessage"] = $"Claim submitted successfully! Claim ID: {claimId}";
                    return RedirectToAction(nameof(Details), new { id = claimId });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error submitting claim: {ex.Message}");
                }
            }

            return View(model);
        }

        // GET: Claims/Edit/5
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var claim = await _claimService.GetClaimByIdAsync(id);

            if (claim == null)
            {
                return NotFound();
            }

            // Only allow editing own claims and only if they're in draft status
            if (claim.LecturerId != currentUser.UserId || claim.Status != ClaimStatus.Draft)
            {
                return Forbid();
            }

            var model = new SubmitClaimViewModel
            {
                HoursWorked = claim.HoursWorked,
                HourlyRate = claim.HourlyRate,
                ClaimPeriod = claim.ClaimPeriod,
                Description = claim.Description
            };

            return View(model);
        }

        // POST: Claims/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Lecturer")]
        public async Task<IActionResult> Edit(int id, SubmitClaimViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await GetCurrentUserAsync();
                    if (currentUser == null) return Unauthorized();

                    var claim = await _claimService.GetClaimByIdAsync(id);

                    if (claim == null || claim.LecturerId != currentUser.UserId || claim.Status != ClaimStatus.Draft)
                    {
                        return Forbid();
                    }

                    claim.HoursWorked = model.HoursWorked;
                    claim.HourlyRate = model.HourlyRate;
                    claim.ClaimPeriod = model.ClaimPeriod;
                    claim.Description = model.Description;

                    await _claimService.UpdateClaimAsync(claim);

                    TempData["SuccessMessage"] = "Claim updated successfully!";
                    return RedirectToAction(nameof(Details), new { id = id });
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating claim: {ex.Message}");
                }
            }

            return View(model);
        }

        // GET: Claims/Approve/5
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Approve(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var claim = await _claimService.GetClaimByIdAsync(id);

            if (claim == null)
            {
                return NotFound();
            }

            // Check if user can approve this claim
            if (!CanUserApproveClaim(claim, currentUser))
            {
                TempData["ErrorMessage"] = "You cannot approve this claim at this time.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var model = new ApproveRejectViewModel
            {
                ClaimId = id,
                Action = "Approve",
                Claim = MapClaimToViewModel(claim, currentUser)
            };

            return View(model);
        }

        // POST: Claims/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Approve(ApproveRejectViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await GetCurrentUserAsync();
                    if (currentUser == null) return Unauthorized();

                    var success = await _claimService.ApproveClaimAsync(model.ClaimId, currentUser.UserId, model.Comments);

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Claim approved successfully!";
                        return RedirectToAction(nameof(Details), new { id = model.ClaimId });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error approving claim. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error approving claim: {ex.Message}");
                }
            }

            // Reload claim data for display
            var claim = await _claimService.GetClaimByIdAsync(model.ClaimId);
            var currentUserForDisplay = await GetCurrentUserAsync();
            model.Claim = MapClaimToViewModel(claim, currentUserForDisplay);

            return View(model);
        }

        // GET: Claims/Reject/5
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Reject(int id)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var claim = await _claimService.GetClaimByIdAsync(id);

            if (claim == null)
            {
                return NotFound();
            }

            // Check if user can reject this claim
            if (!CanUserApproveClaim(claim, currentUser))
            {
                TempData["ErrorMessage"] = "You cannot reject this claim at this time.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var model = new ApproveRejectViewModel
            {
                ClaimId = id,
                Action = "Reject",
                Claim = MapClaimToViewModel(claim, currentUser)
            };

            return View(model);
        }

        // POST: Claims/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Reject(ApproveRejectViewModel model)
        {
            // Comments are required for rejection
            if (string.IsNullOrWhiteSpace(model.Comments))
            {
                ModelState.AddModelError("Comments", "Comments are required when rejecting a claim.");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var currentUser = await GetCurrentUserAsync();
                    if (currentUser == null) return Unauthorized();

                    var success = await _claimService.RejectClaimAsync(model.ClaimId, currentUser.UserId, model.Comments);

                    if (success)
                    {
                        TempData["SuccessMessage"] = "Claim rejected successfully!";
                        return RedirectToAction(nameof(Details), new { id = model.ClaimId });
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error rejecting claim. Please try again.");
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error rejecting claim: {ex.Message}");
                }
            }

            // Reload claim data for display
            var claim = await _claimService.GetClaimByIdAsync(model.ClaimId);
            var currentUserForDisplay = await GetCurrentUserAsync();
            model.Claim = MapClaimToViewModel(claim, currentUserForDisplay);

            return View(model);
        }

        // GET: Claims/Pending
        [Authorize(Roles = "ProgrammeCoordinator,AcademicManager")]
        public async Task<IActionResult> Pending()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null) return Unauthorized();

            var pendingClaims = await _claimService.GetPendingClaimsAsync(currentUser.Role);

            var viewModels = pendingClaims.Select(c => MapClaimToViewModel(c, currentUser)).ToList();
            return View(viewModels);
        }

        // Helper methods

        // FIX: Added the missing GetCurrentUserAsync method
        private async Task<User?> GetCurrentUserAsync()
        {
            // FIX: Added check for authenticated user
            if (User.Identity is not { IsAuthenticated: true })
            {
                return null;
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                // This would be an unauthenticated state, handle as appropriate
                return null;
            }
            return await _userService.GetUserByIdAsync(userId);
        }

        private ClaimViewModel MapClaimToViewModel(Claim claim, User currentUser)
        {
            var viewModel = new ClaimViewModel
            {
                ClaimId = claim.ClaimId,
                LecturerName = claim.Lecturer?.Name ?? "Unknown",
                HoursWorked = claim.HoursWorked,
                HourlyRate = claim.HourlyRate,
                TotalAmount = claim.TotalAmount,
                Status = claim.Status.ToString(),
                SubmissionDate = claim.SubmissionDate,
                ClaimPeriod = claim.ClaimPeriod,
                Description = claim.Description,
                Comments = claim.Comments,
                CanApprove = CanUserApproveClaim(claim, currentUser),
                CanReject = CanUserApproveClaim(claim, currentUser),
                Approvals = claim.Approvals.Select(a => new ApprovalViewModel
                {
                    ApproverName = a.Approver?.Name ?? "Unknown",
                    ApproverRole = a.Approver?.Role.ToString() ?? "Unknown",
                    Status = a.Status.ToString(),
                    ApprovalDate = a.ApprovalDate,
                    Comments = a.Comments
                }).ToList()
            };

            return viewModel;
        }

        private bool CanUserApproveClaim(Claim claim, User user)
        {
            // Can't approve own claims
            if (claim.LecturerId == user.UserId)
                return false;

            // Can only approve submitted or under review claims
            if (claim.Status != ClaimStatus.Submitted && claim.Status != ClaimStatus.UnderReview)
                return false;

            // Check if user has already approved/rejected this claim
            var existingApproval = claim.Approvals.FirstOrDefault(a => a.ApproverId == user.UserId);
            if (existingApproval != null)
                return false;

            // Programme Coordinators can always approve if conditions above are met
            if (user.Role == UserRole.ProgrammeCoordinator)
                return true;

            // Academic Managers can approve if Programme Coordinator has already approved
            if (user.Role == UserRole.AcademicManager)
            {
                return claim.HasApprovalFrom(UserRole.ProgrammeCoordinator);
            }

            return false;
        }
    }
}
using ContractMonthlyClaimSystem.Models.ViewModels;
using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Claim = ContractMonthlyClaimSystem.Models.Claim;

namespace ContractMonthlyClaimSystem.Services
{
    public class ClaimService : IClaimService
    {
        private readonly ApplicationDbContext _context;

        public ClaimService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<int> SubmitClaimAsync(ContractMonthlyClaimSystem.Models.Claim claim)
        {
            // Validation
            if (claim.HoursWorked <= 0 || claim.HoursWorked > 744)
                throw new ArgumentException("Hours worked must be between 0.1 and 744");

            if (claim.HourlyRate <= 0)
                throw new ArgumentException("Hourly rate must be greater than 0");

            // Check for duplicate claims in the same period
            var existingClaim = await _context.Claims
                .Where(c => c.LecturerId == claim.LecturerId
                         && c.ClaimPeriod.Year == claim.ClaimPeriod.Year
                         && c.ClaimPeriod.Month == claim.ClaimPeriod.Month
                         && c.Status != ClaimStatus.Rejected)
                .FirstOrDefaultAsync();

            if (existingClaim != null)
                throw new InvalidOperationException("A claim for this period already exists");

            claim.SubmissionDate = DateTime.Now;
            claim.Status = ClaimStatus.Submitted;

            _context.Claims.Add(claim);
            await _context.SaveChangesAsync();

            return claim.ClaimId;
        }

        public async Task<ContractMonthlyClaimSystem.Models.Claim?> GetClaimByIdAsync(int claimId)
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .FirstOrDefaultAsync(c => c.ClaimId == claimId);
        }

        public async Task<List<ContractMonthlyClaimSystem.Models.Claim>> GetClaimsByLecturerAsync(int lecturerId)
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .Where(c => c.LecturerId == lecturerId)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<List<ContractMonthlyClaimSystem.Models.Claim>> GetPendingClaimsAsync(UserRole approverRole)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .Where(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview);

            if (approverRole == UserRole.ProgrammeCoordinator)
            {
                query = query.Where(c => !c.Approvals.Any(a => a.Approver!.Role == UserRole.ProgrammeCoordinator));
            }
            else if (approverRole == UserRole.AcademicManager)
            {
                query = query.Where(c => c.Approvals.Any(a => a.Approver!.Role == UserRole.ProgrammeCoordinator && a.Status == ApprovalStatus.Approved)
                                        && !c.Approvals.Any(a => a.Approver!.Role == UserRole.AcademicManager));
            }

            return await query.OrderBy(c => c.SubmissionDate).ToListAsync();
        }

        public async Task<List<ContractMonthlyClaimSystem.Models.Claim>> GetAllClaimsAsync()
        {
            return await _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .OrderByDescending(c => c.SubmissionDate)
                .ToListAsync();
        }

        public async Task<bool> ApproveClaimAsync(int claimId, int approverId, string? comments = "")
        {
            var claim = await GetClaimByIdAsync(claimId);
            var approver = await _context.Users.FindAsync(approverId);

            if (claim == null || approver == null)
                return false;

            // Check if approver already acted on this claim
            if (claim.Approvals.Any(a => a.ApproverId == approverId))
                return false;

            // Create approval record
            var approval = new ClaimApproval
            {
                ClaimId = claimId,
                ApproverId = approverId,
                Status = ApprovalStatus.Approved,
                Comments = comments ?? "",
                ApprovalDate = DateTime.Now
            };

            _context.ClaimApprovals.Add(approval);

            // Update claim status
            if (approver.Role == UserRole.ProgrammeCoordinator)
            {
                claim.Status = ClaimStatus.UnderReview;
            }
            else if (approver.Role == UserRole.AcademicManager)
            {
                // Check if Programme Coordinator has already approved
                if (claim.Approvals.Any(a => a.Approver?.Role == UserRole.ProgrammeCoordinator))
                {
                    claim.Status = ClaimStatus.Approved;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectClaimAsync(int claimId, int approverId, string comments)
        {
            if (string.IsNullOrWhiteSpace(comments))
                throw new ArgumentException("Comments are required for rejection");

            var claim = await GetClaimByIdAsync(claimId);
            var approver = await _context.Users.FindAsync(approverId);

            if (claim == null || approver == null)
                return false;

            // Check if approver already acted on this claim
            if (claim.Approvals.Any(a => a.ApproverId == approverId))
                return false;

            // Create approval record
            var approval = new ClaimApproval
            {
                ClaimId = claimId,
                ApproverId = approverId,
                Status = ApprovalStatus.Rejected,
                Comments = comments,
                ApprovalDate = DateTime.Now
            };

            _context.ClaimApprovals.Add(approval);

            // Update claim status
            claim.Status = ClaimStatus.Rejected;
            claim.Comments = comments;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateClaimAsync(ContractMonthlyClaimSystem.Models.Claim claim)
        {
            try
            {
                _context.Claims.Update(claim);
                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<(List<ClaimViewModel> Claims, int TotalCount)> SearchClaimsAsync(ClaimSearchViewModel searchModel, User currentUser)
        {
            var query = _context.Claims
                .Include(c => c.Lecturer)
                .Include(c => c.Approvals)
                    .ThenInclude(a => a.Approver)
                .AsQueryable();

            // Apply filters
            if (searchModel.Status.HasValue)
            {
                query = query.Where(c => c.Status == searchModel.Status.Value);
            }

            if (searchModel.FromDate.HasValue)
            {
                query = query.Where(c => c.SubmissionDate >= searchModel.FromDate.Value);
            }

            if (searchModel.ToDate.HasValue)
            {
                query = query.Where(c => c.SubmissionDate <= searchModel.ToDate.Value.AddDays(1));
            }

            if (searchModel.LecturerId.HasValue)
            {
                query = query.Where(c => c.LecturerId == searchModel.LecturerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchModel.SearchText))
            {
                query = query.Where(c => (c.Description != null && c.Description.Contains(searchModel.SearchText))
                                        || (c.Lecturer != null && c.Lecturer.Name != null && c.Lecturer.Name.Contains(searchModel.SearchText)));
            }

            // For lecturers, only show their own claims
            if (currentUser.Role == UserRole.Lecturer)
            {
                query = query.Where(c => c.LecturerId == currentUser.UserId);
            }

            var totalCount = await query.CountAsync();

            var claims = await query
                .OrderByDescending(c => c.SubmissionDate)
                .Skip((searchModel.Page - 1) * searchModel.PageSize)
                .Take(searchModel.PageSize)
                .Select(c => new ClaimViewModel
                {
                    ClaimId = c.ClaimId,
                    LecturerName = c.Lecturer.Name ?? "N/A",
                    HoursWorked = c.HoursWorked,
                    HourlyRate = c.HourlyRate,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    Status = c.Status.ToString(),
                    SubmissionDate = c.SubmissionDate,
                    ClaimPeriod = c.ClaimPeriod,
                    Description = c.Description ?? string.Empty
                })
                .ToListAsync();

            return (claims, totalCount);
        }

        public async Task<DashboardViewModel> GetDashboardDataAsync(int userId, UserRole userRole)
        {
            var dashboard = new DashboardViewModel();

            if (userRole == UserRole.Lecturer)
            {
                // Lecturer dashboard
                var lecturerClaims = await _context.Claims
                    .Where(c => c.LecturerId == userId)
                    .ToListAsync();

                dashboard.TotalClaims = lecturerClaims.Count;
                dashboard.PendingClaims = lecturerClaims.Count(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview);
                dashboard.ApprovedClaims = lecturerClaims.Count(c => c.Status == ClaimStatus.Approved);
                dashboard.RejectedClaims = lecturerClaims.Count(c => c.Status == ClaimStatus.Rejected);
                dashboard.TotalAmount = lecturerClaims.Sum(c => c.HoursWorked * c.HourlyRate);
                dashboard.ApprovedAmount = lecturerClaims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.HoursWorked * c.HourlyRate);

                dashboard.RecentClaims = lecturerClaims
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(5)
                    .Select(c => new ClaimViewModel
                    {
                        ClaimId = c.ClaimId,
                        LecturerName = "Me",
                        TotalAmount = c.HoursWorked * c.HourlyRate,
                        Status = c.Status.ToString(),
                        SubmissionDate = c.SubmissionDate
                    })
                    .ToList();
            }
            else
            {
                // Approver dashboard
                var allClaims = await _context.Claims
                    .Include(c => c.Lecturer)
                    .ToListAsync();

                dashboard.TotalClaims = allClaims.Count;
                dashboard.PendingClaims = allClaims.Count(c => c.Status == ClaimStatus.Submitted || c.Status == ClaimStatus.UnderReview);
                dashboard.ApprovedClaims = allClaims.Count(c => c.Status == ClaimStatus.Approved);
                dashboard.RejectedClaims = allClaims.Count(c => c.Status == ClaimStatus.Rejected);
                dashboard.TotalAmount = allClaims.Sum(c => c.HoursWorked * c.HourlyRate);
                dashboard.ApprovedAmount = allClaims.Where(c => c.Status == ClaimStatus.Approved).Sum(c => c.HoursWorked * c.HourlyRate);

                dashboard.RecentClaims = allClaims
                    .OrderByDescending(c => c.SubmissionDate)
                    .Take(5)
                    .Select(c => new ClaimViewModel
                    {
                        ClaimId = c.ClaimId,
                        LecturerName = c.Lecturer.Name ?? "N/A",
                        TotalAmount = c.HoursWorked * c.HourlyRate,
                        Status = c.Status.ToString(),
                        SubmissionDate = c.SubmissionDate
                    })
                    .ToList();

                // Claims awaiting approval
                var pendingClaims = await GetPendingClaimsAsync(userRole);
                dashboard.ClaimsAwaitingMyApproval = pendingClaims
                    .Take(5)
                    .Select(c => new ClaimViewModel
                    {
                        ClaimId = c.ClaimId,
                        LecturerName = c.Lecturer.Name ?? "N/A",
                        TotalAmount = c.HoursWorked * c.HourlyRate,
                        Status = c.Status.ToString(),
                        SubmissionDate = c.SubmissionDate
                    })
                    .ToList();
            }

            return dashboard;
        }

        public async Task<List<ClaimSummaryDto>> GetClaimsSummaryAsync(int userId, UserRole userRole)
        {
            var query = _context.Claims.AsQueryable();

            if (userRole == UserRole.Lecturer)
            {
                query = query.Where(c => c.LecturerId == userId);
            }

            return await query
                .Select(c => new ClaimSummaryDto
                {
                    ClaimId = c.ClaimId,
                    TotalAmount = c.HoursWorked * c.HourlyRate,
                    Status = c.Status.ToString(),
                    SubmissionDate = c.SubmissionDate
                })
                .ToListAsync();
        }

        public async Task<object> GetReportsDataAsync()
        {
            // Implementation for reports data
            return new { Message = "Reports data not implemented yet" };
        }

        public async Task<object> GetMonthlyReportAsync(int year, int month)
        {
            // Implementation for monthly report
            return new { Message = "Monthly report not implemented yet" };
        }

        public async Task<object> GetLecturerReportAsync(int lecturerId, int year)
        {
            // Implementation for lecturer report
            return new { Message = "Lecturer report not implemented yet" };
        }
    }
}
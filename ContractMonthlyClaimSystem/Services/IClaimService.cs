
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
    public interface IClaimService
    {
        Task<int> SubmitClaimAsync(Claim claim);
        Task<Claim> GetClaimByIdAsync(int claimId);
        Task<List<Claim>> GetClaimsByLecturerAsync(int lecturerId);
        Task<List<Claim>> GetPendingClaimsAsync(UserRole approverRole);
        Task<List<Claim>> GetAllClaimsAsync();
        Task<bool> ApproveClaimAsync(int claimId, int approverId, string comments = "");
        Task<bool> RejectClaimAsync(int claimId, int approverId, string comments);
        Task<bool> UpdateClaimAsync(Claim claim);
        Task<(List<ClaimViewModel> Claims, int TotalCount)> SearchClaimsAsync(ClaimSearchViewModel searchModel, User currentUser);
        Task<DashboardViewModel> GetDashboardDataAsync(int userId, UserRole userRole);
        Task<List<ClaimSummaryDto>> GetClaimsSummaryAsync(int userId, UserRole userRole);
        Task<object> GetReportsDataAsync();
        Task<object> GetMonthlyReportAsync(int year, int month);
        Task<object> GetLecturerReportAsync(int lecturerId, int year);
    }
}

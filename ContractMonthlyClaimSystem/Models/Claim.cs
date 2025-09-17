using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models
{
    public class Claim
    {
        public int ClaimId { get; set; }

        // Foreign key to User (Lecturer)
        public int LecturerId { get; set; }
        public User Lecturer { get; set; }

        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }

        // ✅ Controller expects this
        public decimal TotalAmount => HoursWorked * HourlyRate;

        public DateTime ClaimPeriod { get; set; }
        public DateTime SubmissionDate { get; set; } = DateTime.Now;

        public string Description { get; set; }
        public string Comments { get; set; }

        public ClaimStatus Status { get; set; } = ClaimStatus.Draft;

        // ✅ Navigation property
        public ICollection<ClaimApproval> Approvals { get; set; } = new List<ClaimApproval>();

        // ✅ Controller calls claim.HasApprovalFrom(UserRole.ProgrammeCoordinator)
        public bool HasApprovalFrom(UserRole role)
        {
            return Approvals != null &&
                   Approvals.Any(a => a.Approver != null &&
                                      a.Approver.Role == role &&
                                      a.Status == ApprovalStatus.Approved);
        }
    }
}


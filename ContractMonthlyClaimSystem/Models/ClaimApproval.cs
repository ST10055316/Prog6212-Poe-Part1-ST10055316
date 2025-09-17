using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models
{
    public class ClaimApproval
    {
        [Key]  // ✅ Explicitly mark this as the primary key
        public int ApprovalId { get; set; }

        public int ClaimId { get; set; }
        public int ApproverId { get; set; }
        public DateTime ApprovalDate { get; set; }
        public ApprovalStatus Status { get; set; }
        public string? Comments { get; set; } // Nullable

        // Navigation properties
        public virtual Claim Claim { get; set; } = null!;
        public virtual User Approver { get; set; } = null!;
    }
}

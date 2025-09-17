using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ClaimViewModel
    {
        public int ClaimId { get; set; }

        [Display(Name = "Lecturer")]
        public string LecturerName { get; set; } = string.Empty;

        [Display(Name = "Hours")]
        public decimal HoursWorked { get; set; }

        [Display(Name = "Rate")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [Display(Name = "Total")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount { get; set; }

        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;

        [Display(Name = "Submitted")]
        [DataType(DataType.Date)]
        public DateTime SubmissionDate { get; set; }

        [Display(Name = "Period")]
        [DataType(DataType.Date)]
        public DateTime ClaimPeriod { get; set; }

        public string Description { get; set; } = string.Empty;
        public string Comments { get; set; } = string.Empty;

        public bool CanApprove { get; set; }
        public bool CanReject { get; set; }

        public List<ApprovalViewModel> Approvals { get; set; } = new List<ApprovalViewModel>();
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class SubmitClaimViewModel
    {
        [Required(ErrorMessage = "Hours worked is required")]
        [Range(0.1, 744, ErrorMessage = "Hours must be between 0.1 and 744 (max hours in a month)")]
        [Display(Name = "Hours Worked")]
        public decimal HoursWorked { get; set; }

        [Required(ErrorMessage = "Hourly rate is required")]
        [Range(0.01, 10000, ErrorMessage = "Hourly rate must be between $0.01 and $10,000")]
        [Display(Name = "Hourly Rate")]
        [DataType(DataType.Currency)]
        public decimal HourlyRate { get; set; }

        [Required(ErrorMessage = "Claim period is required")]
        [Display(Name = "Claim Period")]
        [DataType(DataType.Date)]
        public DateTime ClaimPeriod { get; set; }

        [Required(ErrorMessage = "Description is required")]
        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; } = string.Empty;

        [Display(Name = "Total Amount")]
        [DataType(DataType.Currency)]
        public decimal TotalAmount => HoursWorked * HourlyRate;
    }
}
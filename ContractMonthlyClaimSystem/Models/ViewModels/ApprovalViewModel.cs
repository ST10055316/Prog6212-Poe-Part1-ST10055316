using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ApprovalViewModel
    {
        public string ApproverName { get; set; }
        public string ApproverRole { get; set; }
        public string Status { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string Comments { get; set; }
    }
}

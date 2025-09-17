using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models.ViewModels
{
    public class ClaimSearchViewModel
    {
        [Display(Name = "Status")]
        public ClaimStatus? Status { get; set; }

        [Display(Name = "From Date")]
        [DataType(DataType.Date)]
        public DateTime? FromDate { get; set; }

        [Display(Name = "To Date")]
        [DataType(DataType.Date)]
        public DateTime? ToDate { get; set; }

        [Display(Name = "Lecturer")]
        public int? LecturerId { get; set; }

        [Display(Name = "Search Text")]
        public string SearchText { get; set; }

        // Pagination
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;

        // Results
        public List<ClaimViewModel> Results { get; set; } = new List<ClaimViewModel>();
        public int TotalResults { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalResults / PageSize);

        // Available options
        public List<User> AvailableLecturers { get; set; } = new List<User>();
    }
}


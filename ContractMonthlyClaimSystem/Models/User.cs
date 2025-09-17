using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace ContractMonthlyClaimSystem.Models
{
    // Ensure your models match the migration exactly
    public class User
    {
        public int UserId { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public UserRole Role { get; set; }
        public string Department { get; set; }
        public DateTime CreatedDate { get; set; }

        // Navigation properties
        public ICollection<Claim> Claims { get; set; } = new List<Claim>();
        public ICollection<ClaimApproval> Approvals { get; set; } = new List<ClaimApproval>();
    }
}

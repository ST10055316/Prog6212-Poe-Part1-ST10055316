using ContractMonthlyClaimSystem.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Claim = ContractMonthlyClaimSystem.Models.Claim;

namespace ContractMonthlyClaimSystem.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<ClaimApproval> ClaimApprovals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Make sure required fields are properly configured
            modelBuilder.Entity<User>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(150);
                entity.Property(e => e.Department).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            });

            modelBuilder.Entity<Claim>(entity =>
            {
                entity.Property(e => e.Description).IsRequired().HasMaxLength(500);
                entity.Property(e => e.Comments).HasMaxLength(1000).IsRequired(false); // Make this nullable
            });

            modelBuilder.Entity<ClaimApproval>(entity =>
            {
                entity.Property(e => e.Comments).HasMaxLength(500).IsRequired(false); // Make this nullable
            });

            // Configure relationships
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Lecturer)
                .WithMany(u => u.Claims)
                .HasForeignKey(c => c.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ClaimApproval>()
                .HasOne(ca => ca.Claim)
                .WithMany(c => c.Approvals)
                .HasForeignKey(ca => ca.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<ClaimApproval>()
                .HasOne(ca => ca.Approver)
                .WithMany(u => u.Approvals)
                .HasForeignKey(ca => ca.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed initial data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed Users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    UserId = 1,
                    Name = "Dr. John Smith",
                    Email = "john.smith@university.edu",
                    Username = "john.smith",
                    PasswordHash = HashPassword("password123"),
                    Role = UserRole.Lecturer,
                    Department = "Computer Science",
                    // Fixed: Use a static, hardcoded date instead of DateTime.Now
                    CreatedDate = new DateTime(2025, 3, 15)
                },
                new User
                {
                    UserId = 2,
                    Name = "Prof. Jane Doe",
                    Email = "jane.doe@university.edu",
                    Username = "jane.doe",
                    PasswordHash = HashPassword("password123"),
                    Role = UserRole.ProgrammeCoordinator,
                    Department = "Computer Science",
                    // Fixed: Use a static, hardcoded date
                    CreatedDate = new DateTime(2024, 9, 15)
                },
                new User
                {
                    UserId = 3,
                    Name = "Dr. Mike Johnson",
                    Email = "mike.johnson@university.edu",
                    Username = "mike.johnson",
                    PasswordHash = HashPassword("password123"),
                    Role = UserRole.AcademicManager,
                    Department = "Computer Science",
                    // Fixed: Use a static, hardcoded date
                    CreatedDate = new DateTime(2023, 9, 15)
                }
            );
        }

        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + "SALT"));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}
# Prog6212-Poe-Part1-ST10055316
Contract Monthly Claim System
Overview

The Contract Monthly Claim System is an ASP.NET Core MVC application built to streamline the process of submitting, approving, and managing lecturer contract claims within an academic institution.

It allows Lecturers to submit monthly claims, Programme Coordinators to review them, and Academic Managers to provide final approvals. The system also generates reports and provides dashboards tailored to each user role.

Features

Authentication & Authorization

Secure login and registration with hashed passwords

Role-based access (Lecturer, Programme Coordinator, Academic Manager)

Claims Management

Submit monthly claims with hours worked, rate, and description

Edit claims while in draft

Automated total amount calculation

Approval Workflow

Multi-level approvals (Programme Coordinator → Academic Manager)

Approve/Reject with comments

Prevents self-approval and duplicate approvals

Dashboard & Reporting

Role-based dashboards

Track pending, approved, and rejected claims

Financial summaries for lecturers and management

Reports by lecturer, month, or overall institution

Validation & Security

Strong server-side and client-side validation

Prevents duplicate claims in the same period

Cookie authentication with role claims

Extensibility

Clean separation of concerns (Models, Services, Controllers, ViewModels)

Entity Framework Core for database operations

Seeded demo data for testing

Tech Stack

Framework: ASP.NET Core MVC (.NET 9 / C#)

Database: Entity Framework Core (SQL Server by default)

Authentication: ASP.NET Core Identity (cookie-based)

Frontend: Razor Views, Bootstrap, FontAwesome

Other: Dependency Injection, LINQ, Data Annotations Validation

namespace Refugio.Application.Messages;

using Refugio.Domain.Entities;

public record GetAllDonations() : IFinanceMessage;
public record GetDonationsPaged(int Page, int PageSize = 25) : IFinanceMessage;
public record GetDonationById(int Id) : IFinanceMessage;
public record CreateDonation(string DonorName, decimal Amount, DonationCategory Category, string? Notes) : IFinanceMessage;
public record UpdateDonation(int Id, string DonorName, decimal Amount, DonationCategory Category, string? Notes) : IFinanceMessage;
public record DeleteDonation(int Id) : IFinanceMessage;

public record GetAllExpenses() : IFinanceMessage;
public record GetExpensesPaged(int Page, int PageSize = 25) : IFinanceMessage;
public record GetExpenseById(int Id) : IFinanceMessage;
public record CreateExpense(string Description, decimal Amount, ExpenseCategory Category, string? Notes) : IFinanceMessage;
public record UpdateExpense(int Id, string Description, decimal Amount, ExpenseCategory Category, string? Notes) : IFinanceMessage;
public record DeleteExpense(int Id) : IFinanceMessage;

public record GetFinanceSummary(int Year) : IFinanceMessage;

public record GetAllGoals() : IFinanceMessage;
public record GetGoalById(int Id) : IFinanceMessage;
public record CreateGoal(string Title, string? Description, decimal TargetAmount, decimal CurrentAmount, DateTime? Deadline) : IFinanceMessage;
public record UpdateGoal(int Id, string Title, string? Description, decimal TargetAmount, decimal CurrentAmount, DateTime? Deadline) : IFinanceMessage;
public record DeleteGoal(int Id) : IFinanceMessage;

public record GetDeletedGoals() : IFinanceMessage;
public record RestoreGoal(int Id) : IFinanceMessage;

public record GetDeletedDonations() : IFinanceMessage;
public record RestoreDonation(int Id) : IFinanceMessage;
public record GetDeletedExpenses() : IFinanceMessage;
public record RestoreExpense(int Id) : IFinanceMessage;

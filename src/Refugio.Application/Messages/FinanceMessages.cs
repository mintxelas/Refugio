namespace Refugio.Application.Messages;

using Refugio.Domain.Entities;

public record GetAllDonations();
public record GetDonationsPaged(int Page, int PageSize = 25);
public record DonationPage(List<Donation> Items, int TotalCount, int Page, int PageSize);
public record GetDonationById(int Id);
public record CreateDonation(string DonorName, decimal Amount, DonationCategory Category, string? Notes);
public record UpdateDonation(int Id, string DonorName, decimal Amount, DonationCategory Category, string? Notes);
public record DeleteDonation(int Id);

public record GetAllExpenses();
public record GetExpensesPaged(int Page, int PageSize = 25);
public record ExpensePage(List<Expense> Items, int TotalCount, int Page, int PageSize);
public record GetExpenseById(int Id);
public record CreateExpense(string Description, decimal Amount, ExpenseCategory Category, string? Notes);
public record UpdateExpense(int Id, string Description, decimal Amount, ExpenseCategory Category, string? Notes);
public record DeleteExpense(int Id);

public record GetFinanceSummary(int Year);

public record GetDeletedDonations();
public record RestoreDonation(int Id);
public record GetDeletedExpenses();
public record RestoreExpense(int Id);

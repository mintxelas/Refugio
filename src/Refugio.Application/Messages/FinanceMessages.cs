namespace Refugio.Application.Messages;

using Refugio.Domain.Entities;

public record GetAllDonations();
public record GetDonationById(int Id);
public record CreateDonation(string DonorName, decimal Amount, DonationCategory Category, string? Notes);
public record UpdateDonation(int Id, string DonorName, decimal Amount, DonationCategory Category, string? Notes);
public record DeleteDonation(int Id);

public record GetAllExpenses();
public record GetExpenseById(int Id);
public record CreateExpense(string Description, decimal Amount, string Category, string? Notes);
public record UpdateExpense(int Id, string Description, decimal Amount, string Category, string? Notes);
public record DeleteExpense(int Id);

public record GetFinanceSummary(int Year);

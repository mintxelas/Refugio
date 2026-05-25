namespace Refugio.Application.Messages;

using Refugio.Domain.Entities;

public record GetAllDonations();
public record CreateDonation(string DonorName, decimal Amount, DonationCategory Category, string? Notes);
public record GetAllExpenses();
public record CreateExpense(string Description, decimal Amount, string Category, string? Notes);
public record GetFinanceSummary(int Year);

using Refugio.Domain.Entities;

namespace Refugio.Application.Contracts;

public record DonationDto(
    int Id, string DonorName, decimal Amount, DateTime Date,
    DonationCategory Category, string? Notes, DateTime? DeletedAt, string? TaxId = null);

public record ExpenseTaxLineDto(int Id, decimal IvaPercent, decimal Base, decimal Importe);

public record ExpenseDto(
    int Id, string Description, decimal Amount, DateTime Date,
    ExpenseCategory Category, string? Notes, DateTime? DeletedAt,
    List<ExpensePhotoDto>? Photos = null,
    List<ExpenseTaxLineDto>? TaxLines = null);

public record ExpensePhotoDto(int Id, int ExpenseId, string Url, DateTime UploadedAt);

public record GoalDto(
    int Id, string Title, string? Description, decimal TargetAmount, decimal CurrentAmount,
    DateTime? Deadline, DateTime CreatedAt, DateTime? DeletedAt);

public record TaxLineRequest(decimal IvaPercent, decimal Base, decimal Importe);

public record CreateDonationRequest(string DonorName, decimal Amount, DonationCategory Category, string? Notes, string? TaxId = null);
public record UpdateDonationRequest(int Id, string DonorName, decimal Amount, DonationCategory Category, string? Notes, string? TaxId = null);

public record CreateExpenseRequest(string Description, decimal Amount, ExpenseCategory Category, string? Notes, List<TaxLineRequest>? TaxLines = null);
public record UpdateExpenseRequest(int Id, string Description, decimal Amount, ExpenseCategory Category, string? Notes, List<TaxLineRequest>? TaxLines = null);

public record CreateGoalRequest(string Title, string? Description, decimal TargetAmount, decimal CurrentAmount, DateTime? Deadline);
public record UpdateGoalRequest(int Id, string Title, string? Description, decimal TargetAmount, decimal CurrentAmount, DateTime? Deadline);

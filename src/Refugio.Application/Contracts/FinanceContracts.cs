using Refugio.Domain.Entities;

namespace Refugio.Application.Contracts;

public record DonationDto(
    int Id, string DonorName, decimal Amount, DateTime Date,
    DonationCategory Category, string? Notes, DateTime? DeletedAt);

public record ExpenseDto(
    int Id, string Description, decimal Amount, DateTime Date,
    ExpenseCategory Category, string? Notes, DateTime? DeletedAt,
    List<ExpensePhotoDto>? Photos = null);

public record ExpensePhotoDto(int Id, int ExpenseId, string Url, DateTime UploadedAt);

public record GoalDto(
    int Id, string Title, string? Description, decimal TargetAmount, decimal CurrentAmount,
    DateTime? Deadline, DateTime CreatedAt, DateTime? DeletedAt);

public record CreateDonationRequest(string DonorName, decimal Amount, DonationCategory Category, string? Notes);
public record UpdateDonationRequest(int Id, string DonorName, decimal Amount, DonationCategory Category, string? Notes);

public record CreateExpenseRequest(string Description, decimal Amount, ExpenseCategory Category, string? Notes);
public record UpdateExpenseRequest(int Id, string Description, decimal Amount, ExpenseCategory Category, string? Notes);

public record CreateGoalRequest(string Title, string? Description, decimal TargetAmount, decimal CurrentAmount, DateTime? Deadline);
public record UpdateGoalRequest(int Id, string Title, string? Description, decimal TargetAmount, decimal CurrentAmount, DateTime? Deadline);

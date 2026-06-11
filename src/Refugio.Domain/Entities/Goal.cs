using Refugio.Domain.Common;

namespace Refugio.Domain.Entities;

/// <summary>Aggregate root for a fundraising goal.</summary>
public class Goal : Entity, IAggregateRoot
{
    public string Title { get; private set; } = "";
    public string? Description { get; private set; }
    public decimal TargetAmount { get; private set; }
    public decimal CurrentAmount { get; private set; }
    public DateTime? Deadline { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    private Goal() { }

    public static Goal Create(
        string title, string? description, decimal targetAmount, decimal currentAmount,
        DateTime? deadline = null, DateTime? createdAt = null) => new()
    {
        Title = title,
        Description = description,
        TargetAmount = targetAmount,
        CurrentAmount = currentAmount,
        Deadline = deadline,
        CreatedAt = createdAt ?? DateTime.UtcNow
    };

    public void Update(string title, string? description, decimal targetAmount, decimal currentAmount, DateTime? deadline)
    {
        Title = title;
        Description = description;
        TargetAmount = targetAmount;
        CurrentAmount = currentAmount;
        Deadline = deadline;
    }
}

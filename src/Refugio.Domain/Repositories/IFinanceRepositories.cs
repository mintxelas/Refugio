using Refugio.Domain.Common;
using Refugio.Domain.Entities;

namespace Refugio.Domain.Repositories;

public interface IDonationRepository : IRepository<Donation>
{
    Task<List<Donation>> GetAllAsync();
    Task<Page<Donation>> GetPagedAsync(int page, int pageSize);
}

public interface IExpenseRepository : IRepository<Expense>
{
    Task<List<Expense>> GetAllAsync();
    Task<Page<Expense>> GetPagedAsync(int page, int pageSize);
    Task<bool> ExistsAsync(int expenseId);

    // Receipt photos (children)
    Task<List<ExpensePhoto>> GetPhotosAsync(int expenseId);
    Task<ExpensePhoto?> GetPhotoAsync(int photoId);
    void AddPhoto(ExpensePhoto photo);
    void RemovePhotoPermanently(ExpensePhoto photo);
}

public interface IGoalRepository : IRepository<Goal>
{
    /// <summary>All goals: dated deadlines first (soonest first), then undated, newest created first.</summary>
    Task<List<Goal>> GetAllAsync();
}

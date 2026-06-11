using Microsoft.EntityFrameworkCore;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;
using Refugio.Infrastructure.Data;

namespace Refugio.Infrastructure.Repositories;

public class DonationRepository(ShelterDbContext db) : EfRepository<Donation>(db), IDonationRepository
{
    public Task<List<Donation>> GetAllAsync()
        => Db.Donations.OrderByDescending(d => d.Date).ToListAsync();

    public Task<Page<Donation>> GetPagedAsync(int page, int pageSize)
        => Db.Donations.OrderByDescending(d => d.Date).ToPageAsync(page, pageSize);
}

public class ExpenseRepository(ShelterDbContext db) : EfRepository<Expense>(db), IExpenseRepository
{
    public Task<List<Expense>> GetAllAsync()
        => Db.Expenses.OrderByDescending(e => e.Date).ToListAsync();

    public Task<Page<Expense>> GetPagedAsync(int page, int pageSize)
        => Db.Expenses.OrderByDescending(e => e.Date).ToPageAsync(page, pageSize);

    public Task<bool> ExistsAsync(int expenseId)
        => Db.Expenses.AnyAsync(e => e.Id == expenseId);

    public Task<List<ExpensePhoto>> GetPhotosAsync(int expenseId)
        => Db.ExpensePhotos
            .Where(p => p.ExpenseId == expenseId)
            .OrderByDescending(p => p.UploadedAt)
            .ToListAsync();

    public Task<ExpensePhoto?> GetPhotoAsync(int photoId)
        => Db.ExpensePhotos.FirstOrDefaultAsync(p => p.Id == photoId);

    public void AddPhoto(ExpensePhoto photo) => Db.ExpensePhotos.Add(photo);

    public void RemovePhotoPermanently(ExpensePhoto photo)
    {
        Db.SkipSoftDeleteInterceptor = true;
        Db.ExpensePhotos.Remove(photo);
    }
}

public class GoalRepository(ShelterDbContext db) : EfRepository<Goal>(db), IGoalRepository
{
    public Task<List<Goal>> GetAllAsync()
        => Db.Goals
            .OrderBy(g => g.Deadline == null)
            .ThenBy(g => g.Deadline)
            .ThenByDescending(g => g.CreatedAt)
            .ToListAsync();
}

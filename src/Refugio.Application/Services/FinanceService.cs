using Refugio.Application.Contracts;
using Refugio.Application.Mapping;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;

namespace Refugio.Application.Services;

public interface IFinanceService
{
    // Donations
    Task<List<DonationDto>> GetDonationsAsync();
    Task<Page<DonationDto>> GetDonationsPagedAsync(int page, int pageSize = 25);
    Task<DonationDto?> GetDonationAsync(int id);
    Task<DonationDto> RecordDonationAsync(CreateDonationRequest request);
    Task<DonationDto?> UpdateDonationAsync(UpdateDonationRequest request);
    Task<bool> DeleteDonationAsync(int id);
    Task<bool> RestoreDonationAsync(int id);
    Task<bool> PurgeDonationAsync(int id);
    Task<List<DonationDto>> GetDeletedDonationsAsync();
    Task<DonationDto?> GetDeletedDonationAsync(int id);

    // Expenses
    Task<List<ExpenseDto>> GetExpensesAsync();
    Task<Page<ExpenseDto>> GetExpensesPagedAsync(int page, int pageSize = 25);
    Task<ExpenseDto?> GetExpenseAsync(int id);
    Task<ExpenseDto> RecordExpenseAsync(CreateExpenseRequest request);
    Task<ExpenseDto?> UpdateExpenseAsync(UpdateExpenseRequest request);
    Task<bool> DeleteExpenseAsync(int id);
    Task<bool> RestoreExpenseAsync(int id);
    Task<bool> PurgeExpenseAsync(int id);
    Task<List<ExpenseDto>> GetDeletedExpensesAsync();
    Task<ExpenseDto?> GetDeletedExpenseAsync(int id);
    Task<List<ExpensePhotoDto>> GetExpensePhotosAsync(int expenseId);
    Task<ExpensePhotoDto?> AddExpensePhotoAsync(int expenseId, string url);
    Task<string?> RemoveExpensePhotoAsync(int photoId);

    // Goals
    Task<List<GoalDto>> GetGoalsAsync();
    Task<GoalDto?> GetGoalAsync(int id);
    Task<GoalDto> CreateGoalAsync(CreateGoalRequest request);
    Task<GoalDto?> UpdateGoalAsync(UpdateGoalRequest request);
    Task<bool> DeleteGoalAsync(int id);
    Task<bool> RestoreGoalAsync(int id);
    Task<bool> PurgeGoalAsync(int id);
    Task<List<GoalDto>> GetDeletedGoalsAsync();
    Task<GoalDto?> GetDeletedGoalAsync(int id);
}

/// <summary>Use cases for the finance aggregates: donations, expenses (+ receipts) and goals.</summary>
public class FinanceService(
    IDonationRepository donations,
    IExpenseRepository expenses,
    IGoalRepository goals,
    IUnitOfWork unitOfWork) : ShelterServiceBase(unitOfWork), IFinanceService
{
    // --- Donations ---

    public async Task<List<DonationDto>> GetDonationsAsync()
        => (await donations.GetAllAsync()).Select(d => d.ToDto()).ToList();

    public async Task<Page<DonationDto>> GetDonationsPagedAsync(int page, int pageSize = 25)
        => (await donations.GetPagedAsync(page, pageSize)).ToDto(d => d.ToDto());

    public async Task<DonationDto?> GetDonationAsync(int id)
        => (await donations.GetAsync(id))?.ToDto();

    public async Task<DonationDto> RecordDonationAsync(CreateDonationRequest request)
    {
        var donation = Donation.Record(request.DonorName, request.Amount, request.Category, request.Notes, taxId: request.TaxId);
        donations.Add(donation);
        await UnitOfWork.SaveChangesAsync();
        return donation.ToDto();
    }

    public async Task<DonationDto?> UpdateDonationAsync(UpdateDonationRequest request)
    {
        var donation = await donations.GetAsync(request.Id);
        if (donation is null) return null;
        donation.Update(request.DonorName, request.Amount, request.Category, request.Notes, request.TaxId);
        await UnitOfWork.SaveChangesAsync();
        return donation.ToDto();
    }

    public Task<bool> DeleteDonationAsync(int id) => SoftDeleteAsync(() => donations.GetAsync(id), donations.Remove);
    public Task<bool> RestoreDonationAsync(int id) => RestoreAsync(() => donations.GetDeletedByIdAsync(id));
    public Task<bool> PurgeDonationAsync(int id) => PurgeAsync(() => donations.GetDeletedByIdAsync(id), donations.RemovePermanently);

    public async Task<List<DonationDto>> GetDeletedDonationsAsync()
        => (await donations.GetDeletedAsync()).Select(d => d.ToDto()).ToList();

    public async Task<DonationDto?> GetDeletedDonationAsync(int id)
        => (await donations.GetDeletedByIdAsync(id))?.ToDto();

    // --- Expenses ---

    public async Task<List<ExpenseDto>> GetExpensesAsync()
        => (await expenses.GetAllAsync()).Select(e => e.ToDto()).ToList();

    public async Task<Page<ExpenseDto>> GetExpensesPagedAsync(int page, int pageSize = 25)
        => (await expenses.GetPagedAsync(page, pageSize)).ToDto(e => e.ToDto());

    public async Task<ExpenseDto?> GetExpenseAsync(int id)
        => (await expenses.GetAsync(id))?.ToDto();

    public async Task<ExpenseDto> RecordExpenseAsync(CreateExpenseRequest request)
    {
        var taxLines = ToTaxLineTuples(request.TaxLines);
        var expense = Expense.Record(request.Description, request.Amount, request.Category, taxLines, request.Notes);
        expenses.Add(expense);
        await UnitOfWork.SaveChangesAsync();
        return expense.ToDto();
    }

    public async Task<ExpenseDto?> UpdateExpenseAsync(UpdateExpenseRequest request)
    {
        var expense = await expenses.GetAsync(request.Id);
        if (expense is null) return null;
        var taxLines = ToTaxLineTuples(request.TaxLines);
        expense.Update(request.Description, request.Amount, request.Category, request.Notes, taxLines);
        await UnitOfWork.SaveChangesAsync();
        return expense.ToDto();
    }

    private static IEnumerable<(decimal IvaPercent, decimal Base, decimal Importe)> ToTaxLineTuples(
        List<TaxLineRequest>? lines) =>
        lines?.Select(l => (l.IvaPercent, l.Base, l.Importe))
        ?? throw new ArgumentException("Tax lines are required.");

    public Task<bool> DeleteExpenseAsync(int id) => SoftDeleteAsync(() => expenses.GetAsync(id), expenses.Remove);
    public Task<bool> RestoreExpenseAsync(int id) => RestoreAsync(() => expenses.GetDeletedByIdAsync(id));
    public Task<bool> PurgeExpenseAsync(int id) => PurgeAsync(() => expenses.GetDeletedByIdAsync(id), expenses.RemovePermanently);

    public async Task<List<ExpenseDto>> GetDeletedExpensesAsync()
        => (await expenses.GetDeletedAsync()).Select(e => e.ToDto()).ToList();

    public async Task<ExpenseDto?> GetDeletedExpenseAsync(int id)
        => (await expenses.GetDeletedByIdAsync(id))?.ToDto();

    public async Task<List<ExpensePhotoDto>> GetExpensePhotosAsync(int expenseId)
        => (await expenses.GetPhotosAsync(expenseId)).Select(p => p.ToDto()).ToList();

    public async Task<ExpensePhotoDto?> AddExpensePhotoAsync(int expenseId, string url)
    {
        if (!await expenses.ExistsAsync(expenseId)) return null;
        var photo = ExpensePhoto.Create(expenseId, url);
        expenses.AddPhoto(photo);
        await UnitOfWork.SaveChangesAsync();
        return photo.ToDto();
    }

    /// <summary>Hard-deletes the receipt row; returns its URL so the caller can remove the file.</summary>
    public async Task<string?> RemoveExpensePhotoAsync(int photoId)
    {
        var photo = await expenses.GetPhotoAsync(photoId);
        if (photo is null) return null;
        var url = photo.Url;
        expenses.RemovePhotoPermanently(photo);
        await UnitOfWork.SaveChangesAsync();
        return url;
    }

    // --- Goals ---

    public async Task<List<GoalDto>> GetGoalsAsync()
        => (await goals.GetAllAsync()).Select(g => g.ToDto()).ToList();

    public async Task<GoalDto?> GetGoalAsync(int id)
        => (await goals.GetAsync(id))?.ToDto();

    public async Task<GoalDto> CreateGoalAsync(CreateGoalRequest request)
    {
        var goal = Goal.Create(request.Title, request.Description, request.TargetAmount,
            request.CurrentAmount, request.Deadline);
        goals.Add(goal);
        await UnitOfWork.SaveChangesAsync();
        return goal.ToDto();
    }

    public async Task<GoalDto?> UpdateGoalAsync(UpdateGoalRequest request)
    {
        var goal = await goals.GetAsync(request.Id);
        if (goal is null) return null;
        goal.Update(request.Title, request.Description, request.TargetAmount,
            request.CurrentAmount, request.Deadline);
        await UnitOfWork.SaveChangesAsync();
        return goal.ToDto();
    }

    public Task<bool> DeleteGoalAsync(int id) => SoftDeleteAsync(() => goals.GetAsync(id), goals.Remove);
    public Task<bool> RestoreGoalAsync(int id) => RestoreAsync(() => goals.GetDeletedByIdAsync(id));
    public Task<bool> PurgeGoalAsync(int id) => PurgeAsync(() => goals.GetDeletedByIdAsync(id), goals.RemovePermanently);

    public async Task<List<GoalDto>> GetDeletedGoalsAsync()
        => (await goals.GetDeletedAsync()).Select(g => g.ToDto()).ToList();

    public async Task<GoalDto?> GetDeletedGoalAsync(int id)
        => (await goals.GetDeletedByIdAsync(id))?.ToDto();
}

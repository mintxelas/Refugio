using Refugio.Application.Contracts;
using Refugio.Application.Mapping;
using Refugio.Domain.Common;
using Refugio.Domain.Entities;
using Refugio.Domain.Repositories;

namespace Refugio.Application.Services;

public interface ITaskService
{
    Task<List<ShelterTaskDto>> GetTasksAsync(bool includeCompleted = false);
    Task<ShelterTaskDto> CreateAsync(CreateTaskRequest request);
    Task<bool> CompleteAsync(int id);
    Task<bool> DeleteAsync(int id);
}

public class TaskService(IShelterTaskRepository tasks, IUnitOfWork unitOfWork)
    : ShelterServiceBase(unitOfWork), ITaskService
{
    public async Task<List<ShelterTaskDto>> GetTasksAsync(bool includeCompleted = false)
        => (await tasks.GetAllAsync(includeCompleted)).Select(t => t.ToDto()).ToList();

    public async Task<ShelterTaskDto> CreateAsync(CreateTaskRequest request)
    {
        var task = ShelterTask.Create(
            request.Title, request.DueDateTime, request.Notes,
            request.Location, request.AssignedVolunteerId);
        tasks.Add(task);
        await UnitOfWork.SaveChangesAsync();
        return task.ToDto();
    }

    public async Task<bool> CompleteAsync(int id)
    {
        var task = await tasks.GetAsync(id);
        if (task is null) return false;
        task.Complete();
        await UnitOfWork.SaveChangesAsync();
        return true;
    }

    public Task<bool> DeleteAsync(int id) => SoftDeleteAsync(() => tasks.GetAsync(id), tasks.Remove);
}

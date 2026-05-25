namespace Refugio.Application.Messages;

public record GetAllTasks(bool? IncludeCompleted = false);
public record CreateTask(string Title, DateTime DueDateTime, string? Notes, string? AssignedTo, string? Location);
public record CompleteTask(int Id);
public record DeleteTask(int Id);

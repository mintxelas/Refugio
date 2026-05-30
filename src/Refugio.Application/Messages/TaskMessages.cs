namespace Refugio.Application.Messages;

public record GetAllTasks(bool? IncludeCompleted = false) : ITaskMessage;
public record CreateTask(string Title, DateTime DueDateTime, string? Notes, int? AssignedVolunteerId, string? Location) : ITaskMessage;
public record CompleteTask(int Id) : ITaskMessage;
public record DeleteTask(int Id) : ITaskMessage;

namespace Refugio.Actors.Messages;

// Task-area actor messages. Create reuses CreateTaskRequest from Application.Contracts.
public sealed record GetTasks(bool IncludeCompleted);
public sealed record CompleteTask(int Id);
public sealed record DeleteTask(int Id);

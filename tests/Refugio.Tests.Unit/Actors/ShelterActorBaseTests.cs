using Akka.Actor;
using Akka.TestKit.Xunit2;
using Microsoft.Extensions.DependencyInjection;
using Refugio.Actors;

namespace Refugio.Tests.Unit.Actors;

/// <summary>
/// Proves the actor-layer contract: scope-per-message delegation, NullReply unwrapping,
/// failure propagation through Ask, command serialization, and query concurrency.
/// </summary>
public class ShelterActorBaseTests : TestKit
{
    public interface IProbeService
    {
        Task<string?> FindAsync(int id);
        Task<int> SlowCommandAsync();
        Task<string> FailAsync();
    }

    private sealed class ProbeService : IProbeService
    {
        public readonly TaskCompletionSource FirstEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource BothEntered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource Gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int _entries;

        public int Entries => Volatile.Read(ref _entries);

        public async Task<string?> FindAsync(int id)
        {
            if (id == 0) return null;
            if (id < 0)
            {
                var n = Interlocked.Increment(ref _entries);
                if (n == 2) BothEntered.TrySetResult();
                await Gate.Task;
            }
            return $"item-{id}";
        }

        public async Task<int> SlowCommandAsync()
        {
            var n = Interlocked.Increment(ref _entries);
            if (n == 1) FirstEntered.TrySetResult();
            await Gate.Task;
            return n;
        }

        public Task<string> FailAsync() => throw new InvalidOperationException("boom");
    }

    private sealed record Find(int Id);
    private sealed record SlowCommand;
    private sealed record Fail;
    private sealed record FailQuery;

    private sealed class ProbeActor : ShelterActorBase<IProbeService>
    {
        public ProbeActor(IServiceScopeFactory scopeFactory) : base(scopeFactory)
        {
            Query<Find>(async (s, m) => await s.FindAsync(m.Id));
            Command<SlowCommand>(async (s, _) => await s.SlowCommandAsync());
            Command<Fail>(async (s, _) => await s.FailAsync());
            Query<FailQuery>(async (s, _) => await s.FailAsync());
        }
    }

    private readonly ProbeService _service = new();
    private readonly IActorRef _actor;

    public ShelterActorBaseTests()
    {
        var scopeFactory = new ServiceCollection()
            .AddSingleton<IProbeService>(_service)
            .BuildServiceProvider()
            .GetRequiredService<IServiceScopeFactory>();
        _actor = Sys.ActorOf(Props.Create(() => new ProbeActor(scopeFactory)));
    }

    [Fact]
    public async Task Query_returns_service_result()
    {
        var result = await _actor.AskFor<string>(new Find(7));
        Assert.Equal("item-7", result);
    }

    [Fact]
    public async Task Query_null_result_unwraps_to_null()
    {
        var result = await _actor.AskFor<string>(new Find(0));
        Assert.Null(result);
    }

    [Fact]
    public async Task Command_failure_faults_the_ask()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _actor.AskFor<string>(new Fail()));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Query_failure_faults_the_ask()
    {
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _actor.AskFor<string>(new FailQuery()));
        Assert.Equal("boom", ex.Message);
    }

    [Fact]
    public async Task Commands_are_serialized_per_actor()
    {
        var first = _actor.AskFor<int>(new SlowCommand());
        var second = _actor.AskFor<int>(new SlowCommand());

        await _service.FirstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await Task.Delay(200);
        Assert.Equal(1, _service.Entries); // second command waits in the mailbox

        _service.Gate.SetResult();
        Assert.Equal(1, await first);
        Assert.Equal(2, await second);
    }

    [Fact]
    public async Task Queries_run_concurrently()
    {
        var first = _actor.AskFor<string>(new Find(-1));
        var second = _actor.AskFor<string>(new Find(-2));

        // Both queries enter the service before either completes — would time out if serialized.
        await _service.BothEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        _service.Gate.SetResult();
        Assert.Equal("item--1", await first);
        Assert.Equal("item--2", await second);
    }
}

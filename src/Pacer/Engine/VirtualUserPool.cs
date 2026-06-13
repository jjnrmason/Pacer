namespace Pacer.Engine;

/// <summary>
/// Manages the set of running virtual-user worker tasks for one load phase. Scaling up starts new
/// workers; scaling down cancels the most recently added ones. Each worker is created from a factory
/// that takes the virtual-user id and a per-worker cancellation token.
/// </summary>
internal sealed class VirtualUserPool : IAsyncDisposable
{
    private readonly Func<int, CancellationToken, Task> _workerFactory;
    private readonly List<Worker> _workers = [];
    private readonly object _gate = new();
    private int _nextId;

    public VirtualUserPool(Func<int, CancellationToken, Task> workerFactory)
    {
        ArgumentNullException.ThrowIfNull(workerFactory);
        _workerFactory = workerFactory;
    }

    /// <summary>The number of workers currently running.</summary>
    public int Count
    {
        get
        {
            lock (_gate)
            {
                return _workers.Count;
            }
        }
    }

    /// <summary>Adds or removes workers so that exactly <paramref name="target"/> are running.</summary>
    public void ScaleTo(int target)
    {
        if (target < 0)
            target = 0;

        lock (_gate)
        {
            while (_workers.Count < target)
            {
                var id = _nextId++;
                var cts = new CancellationTokenSource();
                var task = Task.Run(() => _workerFactory(id, cts.Token), CancellationToken.None);
                _workers.Add(new Worker(task, cts));
            }

            while (_workers.Count > target)
            {
                var last = _workers[^1];
                _workers.RemoveAt(_workers.Count - 1);
                last.Cancellation.Cancel();
            }
        }
    }

    /// <summary>Cancels all workers and waits for them to finish, up to <paramref name="grace"/>.</summary>
    public async Task StopAsync(TimeSpan grace, TimeProvider timeProvider)
    {
        Worker[] snapshot;
        lock (_gate)
        {
            snapshot = [.. _workers];
            _workers.Clear();
        }

        foreach (var worker in snapshot)
            worker.Cancellation.Cancel();

        var all = Task.WhenAll(snapshot.Select(w => w.Task));
        var graceDelay = Task.Delay(grace, timeProvider, CancellationToken.None);
        await Task.WhenAny(all, graceDelay).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        Worker[] snapshot;
        lock (_gate)
        {
            snapshot = [.. _workers];
            _workers.Clear();
        }

        foreach (var worker in snapshot)
        {
            worker.Cancellation.Cancel();
            worker.Cancellation.Dispose();
        }

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private sealed record Worker(Task Task, CancellationTokenSource Cancellation);
}

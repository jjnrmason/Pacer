using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Pacer.Load;
using Pacer.Metrics;
using Pacer.Scenarios;

namespace Pacer.Engine;

/// <summary>
/// Runs a single <see cref="ScenarioDefinition"/> end to end — optional setup, optional warm-up,
/// the measured load phase driven by the scenario's load profile, then teardown — and produces a
/// <see cref="RunReport"/>. All timing flows through an injected <see cref="TimeProvider"/> so runs
/// are deterministic under test.
/// </summary>
public sealed class ScenarioRunner
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan StopGrace = TimeSpan.FromSeconds(5);
    private const int BaseSeed = 17;

    private readonly IServiceProvider _services;
    private readonly ILoggerFactory _loggerFactory;
    private readonly TimeProvider _timeProvider;
    private readonly PacerMeter _meter;

    /// <summary>Creates a scenario runner.</summary>
    public ScenarioRunner(IServiceProvider services, ILoggerFactory loggerFactory, TimeProvider timeProvider, PacerMeter meter)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ArgumentNullException.ThrowIfNull(timeProvider);
        ArgumentNullException.ThrowIfNull(meter);

        _services = services;
        _loggerFactory = loggerFactory;
        _timeProvider = timeProvider;
        _meter = meter;
    }

    /// <summary>Runs the scenario and returns its report.</summary>
    public async Task<RunReport> RunAsync(ScenarioDefinition scenario, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scenario);

        var logger = _loggerFactory.CreateLogger($"Pacer.{scenario.Name}");
        var setupContext = new SetupContext(_services, logger, scenario.Name);

        object? scenarioData = null;
        if (scenario.Setup is not null)
        {
            logger.LogInformation("Running setup for '{Scenario}'.", scenario.Name);
            scenarioData = await scenario.Setup(setupContext, cancellationToken).ConfigureAwait(false);
        }

        try
        {
            if (scenario.Warmup > TimeSpan.Zero)
            {
                logger.LogInformation("Warming up '{Scenario}' for {Warmup}.", scenario.Name, scenario.Warmup);
                await RunWarmupAsync(scenario, scenarioData, logger, cancellationToken).ConfigureAwait(false);
            }

            logger.LogInformation(
                "Running '{Scenario}' with the {Profile} profile (peak {Peak} users) for {Duration}.",
                scenario.Name, scenario.Load.Kind, scenario.Load.PeakUsers, scenario.Load.TotalDuration);

            return await RunMeasuredAsync(scenario, scenarioData, logger, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (scenario.Teardown is not null)
                await scenario.Teardown(setupContext, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunWarmupAsync(ScenarioDefinition scenario, object? scenarioData, ILogger logger, CancellationToken cancellationToken)
    {
        var users = Math.Max(1, scenario.Load.TargetUsersAt(TimeSpan.Zero));
        var warmupProfile = LoadProfiles.Load(users, scenario.Warmup);
        await RunPhaseAsync(scenario, scenarioData, logger, warmupProfile, recorder: NoOpRecorder.Instance, intervals: null, cancellationToken).ConfigureAwait(false);
    }

    private async Task<RunReport> RunMeasuredAsync(ScenarioDefinition scenario, object? scenarioData, ILogger logger, CancellationToken cancellationToken)
    {
        var collectors = scenario.Steps.ToDictionary(s => s.Name, s => new StepStatsCollector(s.Name), StringComparer.Ordinal);
        var journeyCollector = new StepStatsCollector(scenario.Name);
        var recorder = new CollectorRecorder(collectors, _meter, scenario.Name);
        var intervals = new List<IntervalSnapshot>();

        var startedAt = _timeProvider.GetUtcNow();
        await RunPhaseAsync(scenario, scenarioData, logger, scenario.Load, recorder, intervals, cancellationToken, journeyCollector).ConfigureAwait(false);

        var window = scenario.Load.TotalDuration;
        return new RunReport
        {
            ScenarioName = scenario.Name,
            Group = scenario.Group,
            LoadProfileKind = scenario.Load.Kind,
            PeakUsers = scenario.Load.PeakUsers,
            StartedAt = startedAt,
            Duration = window,
            Steps = [.. scenario.Steps.Select(s => collectors[s.Name].Snapshot(window))],
            Journey = journeyCollector.Snapshot(window),
            Intervals = intervals,
            Environment = EnvironmentInfo.Capture(),
        };
    }

    private async Task RunPhaseAsync(
        ScenarioDefinition scenario,
        object? scenarioData,
        ILogger logger,
        LoadProfile profile,
        IStepRecorder recorder,
        List<IntervalSnapshot>? intervals,
        CancellationToken cancellationToken,
        StepStatsCollector? journeyCollector = null)
    {
        using var phaseCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = phaseCts.Token;

        await using var pool = new VirtualUserPool((id, ct) =>
            WorkerLoopAsync(id, scenario, scenarioData, logger, recorder, journeyCollector, ct));

        var startTimestamp = _timeProvider.GetTimestamp();
        var reportedUsers = 0;
        long lastTotal = 0;

        void Reconcile(bool capture)
        {
            var elapsed = _timeProvider.GetElapsedTime(startTimestamp);
            var target = profile.TargetUsersAt(elapsed >= profile.TotalDuration ? profile.TotalDuration : elapsed);
            pool.ScaleTo(target);

            if (target != reportedUsers)
            {
                _meter.AddActiveUsers(scenario.Name, target - reportedUsers);
                reportedUsers = target;
            }

            if (capture && intervals is not null && recorder is CollectorRecorder cr)
            {
                var total = cr.Ok + cr.Fail;
                var delta = total - lastTotal;
                lastTotal = total;
                intervals.Add(new IntervalSnapshot(elapsed, target, cr.Ok, cr.Fail, delta / TickInterval.TotalSeconds));
            }
        }

        Reconcile(capture: false);

        using var timer = _timeProvider.CreateTimer(_ => Reconcile(capture: true), null, TickInterval, TickInterval);

        await Task.Delay(profile.TotalDuration, _timeProvider, cancellationToken).ConfigureAwait(false);

        await pool.StopAsync(StopGrace, _timeProvider).ConfigureAwait(false);

        if (reportedUsers != 0)
        {
            _meter.AddActiveUsers(scenario.Name, -reportedUsers);
            reportedUsers = 0;
        }
    }

    private async Task WorkerLoopAsync(
        int virtualUserId,
        ScenarioDefinition scenario,
        object? scenarioData,
        ILogger logger,
        IStepRecorder recorder,
        StepStatsCollector? journeyCollector,
        CancellationToken cancellationToken)
    {
        var random = new Random(unchecked(BaseSeed + virtualUserId));
        var context = new StepContext(scenarioData, virtualUserId, random, logger, _services);
        long invocation = 0;

        while (!cancellationToken.IsCancellationRequested)
        {
            invocation++;
            await using var scope = _services.CreateAsyncScope();
            context.Services = scope.ServiceProvider;
            context.InvocationNumber = invocation;
            context.CancellationToken = cancellationToken;

            JourneyOutcome outcome;
            try
            {
                outcome = await JourneyRunner.RunAsync(scenario.Steps, context, _timeProvider, recorder, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            journeyCollector?.Record(outcome.Elapsed, outcome.IsOk);
        }
    }
}

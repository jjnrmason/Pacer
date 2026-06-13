using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Pacer.Engine;
using Pacer.Load;
using Pacer.Metrics;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pacer.Tests.Engine.WhenRunningAScenario;

public class ScenarioRunnerTestBase
{
    protected FakeTimeProvider TimeProvider { get; private set; } = null!;
    protected PacerMeter Meter { get; private set; } = null!;
    protected ScenarioRunner ScenarioRunner { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        this.TimeProvider = new FakeTimeProvider();
        this.Meter = new PacerMeter();
        var services = new ServiceCollection().BuildServiceProvider();
        this.ScenarioRunner = new ScenarioRunner(services, NullLoggerFactory.Instance, this.TimeProvider, this.Meter);
    }

    [TearDown]
    public void TearDown()
    {
        this.Meter.Dispose();
    }

    protected static ScenarioDefinition TwoStepScenario(LoadProfile load, Action? onStep = null)
    {
        return Scenario.Create("checkout")
            .InGroup("ecommerce")
            .AddStep("login", (_, _) =>
            {
                onStep?.Invoke();
                return ValueTask.FromResult(StepResult.Ok());
            })
            .AddStep("purchase", (_, _) => ValueTask.FromResult(StepResult.Ok()))
            .WithLoad(load)
            .Build();
    }

    /// <summary>
    /// Runs the scenario, gives the worker tasks a brief real-time head start to execute
    /// journeys, then advances the fake clock past the load duration so the run completes.
    /// </summary>
    protected async Task<RunReport> RunToCompletionAsync(ScenarioDefinition scenario)
    {
        var runTask = this.ScenarioRunner.RunAsync(scenario);
        await Task.Delay(100);
        this.TimeProvider.Advance(scenario.Load.TotalDuration + TimeSpan.FromSeconds(1));
        return await runTask;
    }
}
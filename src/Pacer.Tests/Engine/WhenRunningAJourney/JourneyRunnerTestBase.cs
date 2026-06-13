using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Pacer.Engine;
using Pacer.Steps;

namespace Pacer.Tests.Engine.WhenRunningAJourney;

public class JourneyRunnerTestBase
{
    protected FakeTimeProvider TimeProvider { get; private set; } = null!;
    protected CapturingRecorder Recorder { get; private set; } = null!;
    internal StepContext Context { get; private set; } = null!;

    [SetUp]
    public void SetUp()
    {
        this.TimeProvider = new FakeTimeProvider();
        this.Recorder = new CapturingRecorder();
        var services = new ServiceCollection().BuildServiceProvider();
        this.Context = new StepContext(scenarioData: null, virtualUserId: 0, random: new Random(0), logger: NullLogger.Instance, services: services);
    }

    protected static Step OkStep(string name, object? payload = null)
    {
        return new Step(name, (_, _) => ValueTask.FromResult(StepResult.Ok(payload: payload)));
    }

    protected static Step FailStep(string name)
    {
        return new Step(name, (_, _) => ValueTask.FromResult(StepResult.Fail()));
    }

    protected static Step ThrowStep(string name)
    {
        return new Step(name, (_, _) => throw new InvalidOperationException("boom"));
    }

    protected sealed class CapturingRecorder : IStepRecorder
    {
        public List<(string Name, StepResult Result)> Records { get; } = [];

        public void Record(string stepName, TimeSpan latency, StepResult result)
        {
            this.Records.Add((stepName, result));
        }
    }
}
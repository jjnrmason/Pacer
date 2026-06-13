using Pacer.Steps;

namespace Pacer.Engine;

/// <summary>Receives the outcome of each step execution so it can be measured and reported.</summary>
internal interface IStepRecorder
{
    /// <summary>Records a single step execution.</summary>
    void Record(string stepName, TimeSpan latency, StepResult result);
}

/// <summary>A recorder that discards everything — used during the warm-up phase.</summary>
internal sealed class NoOpRecorder : IStepRecorder
{
    public static readonly NoOpRecorder Instance = new();

    private NoOpRecorder()
    {
    }

    public void Record(string stepName, TimeSpan latency, StepResult result)
    {
    }
}

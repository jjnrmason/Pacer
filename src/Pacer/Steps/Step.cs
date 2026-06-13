namespace Pacer.Steps;

/// <summary>The work a step performs, returning its <see cref="StepResult"/>.</summary>
public delegate ValueTask<StepResult> StepDelegate(IStepContext context, CancellationToken cancellationToken);

/// <summary>
/// A named, measured unit of work within a scenario. Steps run in declaration order as a pipeline
/// for each virtual user; the framework times each one individually.
/// </summary>
public sealed class Step
{
    /// <summary>The step's name, unique within its scenario. Used as the metric/report key.</summary>
    public string Name { get; }

    /// <summary>The work the step performs.</summary>
    public StepDelegate Execute { get; }

    /// <summary>Creates a step.</summary>
    public Step(string name, StepDelegate execute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(execute);

        Name = name;
        Execute = execute;
    }
}

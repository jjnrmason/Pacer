using Pacer.Load;
using Pacer.Steps;

namespace Pacer.Scenarios;

/// <summary>
/// A fluent builder for a <see cref="ScenarioDefinition"/>. Start with <see cref="Create"/>, add
/// steps and configuration, then call <see cref="Build"/> (the engine and registry call it for you).
/// </summary>
/// <example>
/// A three-step checkout journey with inline one-time setup, a group, and a ramp (bell-curve) load.
/// The setup runs once and its result is read by every step via
/// <see cref="Steps.StepContextExtensions.ScenarioDataAs{T}"/>:
/// <code><![CDATA[
/// var checkout = Scenario.Create("checkout")
///     .InGroup("storefront")
///     .WithSetup(async ctx => new CheckoutData(AuthToken: "token-abc", ProductIds: [1000, 1001, 1002]))
///     .WithWarmup(TimeSpan.FromSeconds(2))
///     .AddStep("login", async ctx =>
///     {
///         var data = ctx.ScenarioDataAs<CheckoutData>()!;
///         return StepResult.Ok(payload: data.AuthToken);
///     })
///     .AddStep("browse", async ctx => StepResult.Ok(sizeBytes: 4096))
///     .AddStep("purchase", async ctx => StepResult.Ok())
///     .WithLoad(LoadProfiles.Ramp(
///         peak: 50,
///         rampUp: TimeSpan.FromSeconds(10),
///         hold: TimeSpan.FromSeconds(20),
///         rampDown: TimeSpan.FromSeconds(10)));
/// ]]></code>
/// </example>
public sealed class Scenario
{
    private readonly string _name;
    private readonly List<Step> _steps = [];
    private readonly HashSet<string> _stepNames = new(StringComparer.Ordinal);
    private string? _group;
    private ScenarioSetupDelegate? _setup;
    private ScenarioTeardownDelegate? _teardown;
    private TimeSpan _warmup = TimeSpan.Zero;
    private LoadProfile? _load;

    private Scenario(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        _name = name;
    }

    /// <summary>Begins defining a scenario with the given unique name.</summary>
    public static Scenario Create(string name) => new(name);

    /// <summary>Assigns the scenario to a group so it can be run via a group selector.</summary>
    public Scenario InGroup(string group)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(group);
        _group = group;
        return this;
    }

    /// <summary>
    /// Defines the one-time setup that runs before the load phase. Its return value is exposed to
    /// every step via <see cref="IStepContext.ScenarioData"/>. Resolve dependencies through
    /// <see cref="ISetupContext.Services"/>.
    /// </summary>
    public Scenario WithSetup(ScenarioSetupDelegate setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        _setup = setup;
        return this;
    }

    /// <summary>Defines the one-time setup that produces strongly-typed scenario data.</summary>
    public Scenario WithSetup<TData>(Func<ISetupContext, CancellationToken, ValueTask<TData>> setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        ScenarioSetupDelegate wrapped = async (context, cancellationToken) => (object?)await setup(context, cancellationToken).ConfigureAwait(false);
        return WithSetup(wrapped);
    }

    /// <summary>Defines the one-time setup that produces strongly-typed scenario data.</summary>
    public Scenario WithSetup<TData>(Func<ISetupContext, ValueTask<TData>> setup)
    {
        ArgumentNullException.ThrowIfNull(setup);
        return WithSetup<TData>((context, _) => setup(context));
    }

    /// <summary>Defines the one-time teardown that runs after the load phase completes.</summary>
    public Scenario WithTeardown(ScenarioTeardownDelegate teardown)
    {
        ArgumentNullException.ThrowIfNull(teardown);
        _teardown = teardown;
        return this;
    }

    /// <summary>Appends a step. Step names must be unique within the scenario.</summary>
    public Scenario AddStep(string name, StepDelegate execute)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(execute);
        if (!_stepNames.Add(name))
            throw new ArgumentException($"A step named '{name}' already exists in scenario '{_name}'.", nameof(name));

        _steps.Add(new Step(name, execute));
        return this;
    }

    /// <summary>Appends a step using a delegate that does not take the cancellation token directly.</summary>
    public Scenario AddStep(string name, Func<IStepContext, ValueTask<StepResult>> execute)
    {
        ArgumentNullException.ThrowIfNull(execute);
        return AddStep(name, (context, _) => execute(context));
    }

    /// <summary>Sets how long to run un-recorded warm-up journeys before measurement begins.</summary>
    public Scenario WithWarmup(TimeSpan warmup)
    {
        if (warmup < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(warmup), warmup, "Warm-up duration cannot be negative.");
        _warmup = warmup;
        return this;
    }

    /// <summary>Sets the load profile that drives virtual-user concurrency.</summary>
    public Scenario WithLoad(LoadProfile load)
    {
        ArgumentNullException.ThrowIfNull(load);
        _load = load;
        return this;
    }

    /// <summary>Validates the configuration and produces an immutable <see cref="ScenarioDefinition"/>.</summary>
    public ScenarioDefinition Build()
    {
        if (_steps.Count == 0)
            throw new InvalidOperationException($"Scenario '{_name}' must define at least one step.");
        if (_load is null)
            throw new InvalidOperationException($"Scenario '{_name}' must define a load profile via {nameof(WithLoad)}.");

        return new ScenarioDefinition(_name, _group, _steps.ToArray(), _setup, _teardown, _warmup, _load);
    }
}

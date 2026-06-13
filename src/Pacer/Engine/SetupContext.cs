using Microsoft.Extensions.Logging;
using Pacer.Scenarios;

namespace Pacer.Engine;

/// <summary>The implementation of <see cref="ISetupContext"/> passed to scenario setup and teardown.</summary>
internal sealed class SetupContext : ISetupContext
{
    public SetupContext(IServiceProvider services, ILogger logger, string scenarioName)
    {
        Services = services;
        Logger = logger;
        ScenarioName = scenarioName;
    }

    public IServiceProvider Services { get; }

    public ILogger Logger { get; }

    public string ScenarioName { get; }
}

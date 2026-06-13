using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Pacer.Engine;
using Pacer.Metrics;
using Pacer.Reporting;
using Pacer.Scenarios;

namespace Pacer.Hosting;

/// <summary>Registers Pacer's services and returns a builder for registering scenarios.</summary>
public static class PacerServiceCollectionExtensions
{
    /// <summary>
    /// Adds the Pacer engine, metrics, scenario registry, and the console/CSV/HTML report writers to
    /// the service collection. Returns a <see cref="PacerBuilder"/> for registering scenarios and
    /// setup classes.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// builder.Services.AddPacer()
    ///     .AddScenario(SampleScenarios.Checkout())
    ///     .AddScenario(SampleScenarios.Search());
    /// ]]></code>
    /// </example>
    public static PacerBuilder AddPacer(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton(sp => new PacerMeter(sp.GetService<IMeterFactory>()));
        services.TryAddSingleton<ScenarioRunner>();
        services.TryAddSingleton<TestRunner>();

        var registry = new ScenarioRegistry();
        services.TryAddSingleton(registry);

        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportWriter, ConsoleReportWriter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportWriter, CsvReportWriter>());
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IReportWriter, HtmlReportWriter>());

        return new PacerBuilder(services, registry);
    }
}

/// <summary>A small builder for registering scenarios and report writers with Pacer.</summary>
public sealed class PacerBuilder
{
    private readonly ScenarioRegistry _registry;

    internal PacerBuilder(IServiceCollection services, ScenarioRegistry registry)
    {
        Services = services;
        _registry = registry;
    }

    /// <summary>The underlying service collection, for registering step dependencies.</summary>
    public IServiceCollection Services { get; }

    /// <summary>Registers a built scenario definition.</summary>
    public PacerBuilder AddScenario(ScenarioDefinition scenario)
    {
        _registry.Add(scenario);
        return this;
    }

    /// <summary>Builds and registers a scenario from a fluent builder.</summary>
    public PacerBuilder AddScenario(Scenario scenario)
    {
        _registry.Add(scenario);
        return this;
    }

    /// <summary>
    /// Registers a custom <see cref="IReportWriter"/>. It runs after every test run alongside the
    /// built-in console/CSV/HTML writers.
    /// </summary>
    /// <example>
    /// <code><![CDATA[
    /// builder.Services.AddPacer()
    ///     .AddScenario(MyScenarios.Checkout())
    ///     .AddReportWriter<JsonReportWriter>();
    /// ]]></code>
    /// </example>
    public PacerBuilder AddReportWriter<TWriter>() where TWriter : class, IReportWriter
    {
        Services.AddSingleton<IReportWriter, TWriter>();
        return this;
    }

    /// <summary>Registers a custom <see cref="IReportWriter"/> instance.</summary>
    public PacerBuilder AddReportWriter(IReportWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        Services.AddSingleton(writer);
        return this;
    }
}

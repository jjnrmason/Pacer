![Pacer logo](https://raw.githubusercontent.com/jjnrmason/Pacer/main/docs/logo.png)

# Pacer

**Load testing for .NET, without the baggage.**

[![NuGet](https://img.shields.io/nuget/v/Pacer.svg?logo=nuget&label=NuGet)](https://www.nuget.org/packages/Pacer)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](#license)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4?logo=dotnet&logoColor=white)](#)

An open-source performance and load testing framework for the .NET ecosystem, built **strictly on
first-party Microsoft packages** — no third-party runtime dependencies.

Pacer lets you describe a test as a **scenario**: a pipeline of measured **steps** that each virtual
user runs as a journey, repeated under a realistic **load profile**. It reports throughput, latency
percentiles and error rates to the console, CSV, and a self-contained HTML page, and exposes live
metrics through .NET's `System.Diagnostics.Metrics` so you can watch a run with `dotnet-counters`.

## Features

- **Fluent scenario API** with one-time setup, a data pipeline between steps, and per-virtual-user state.
- **Five built-in load profiles** (closed model — you control the number of virtual users):
  - `Load` — constant users for a duration.
  - `Soak` — constant users held for a long duration (endurance).
  - `Spike` — a baseline interrupted by a sudden surge.
  - `Stress` — a staircase that steps users up to find the breaking point.
  - `Ramp` — a bell curve: ramp up, hold, ramp down.
- **First-party stack**: `Microsoft.Extensions.Hosting`, dependency injection, logging, and `System.CommandLine`.
- **Live metrics** via a `Meter("Pacer")` plus bounded-memory HDR-style latency percentiles for reports.
- **CSV, console and HTML reports** out of the box, plus pluggable custom report writers.
- **Deterministic engine** built on `TimeProvider`, so it is fully unit-tested without real delays.

## Quick start

Create a console app, reference `Pacer`, and hand your arguments to `PacerApplication`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using Pacer.Hosting;
using Pacer.Load;
using Pacer.Scenarios;
using Pacer.Steps;

return await PacerApplication.RunAsync(args, builder =>
{
    builder.Services.AddPacer()
        .AddScenario(Scenario.Create("checkout")
            .InGroup("storefront")
            .AddStep("login",    async ctx => StepResult.Ok(payload: "token"))
            .AddStep("purchase", async ctx => StepResult.Ok())
            .WithLoad(LoadProfiles.Ramp(
                peak: 100,
                rampUp:   TimeSpan.FromSeconds(30),
                hold:     TimeSpan.FromSeconds(60),
                rampDown: TimeSpan.FromSeconds(30))));
});
```

Then run it:

```bash
dotnet run -- list
dotnet run -- run --scenario checkout --out ./artifacts
dotnet run -- run --group storefront --profile stress --users 200 --duration 5
```

### One-time setup

Setup is defined inline on the scenario, so each scenario is self-contained. `WithSetup` runs once
before the load phase and its return value is exposed to every step via `ctx.ScenarioData`
(read it with `ctx.ScenarioDataAs<T>()`). Resolve dependencies through `ctx.Services`:

```csharp
Scenario.Create("checkout")
    .WithSetup(async ctx =>
    {
        // resolve services via ctx.Services, prepare auth tokens, seed data, …
        return new CheckoutData(AuthToken: "token-abc", ProductIds: [1000, 1001]);
    })
    .AddStep("login", async ctx =>
    {
        var data = ctx.ScenarioDataAs<CheckoutData>()!;
        return StepResult.Ok(payload: data.AuthToken);
    })
    /* … */;
```

## CLI options (`run`)

| Option | Alias | Description |
| --- | --- | --- |
| `--scenario` | `-s` | Run a single named scenario. |
| `--group` | `-g` | Run every scenario in a group. |
| `--all` | | Run every registered scenario. |
| `--users` | `-u` | Override the peak number of virtual users. |
| `--duration` | `-d` | Override the duration, in minutes. |
| `--profile` | `-p` | Override the shape: `load`, `soak`, `spike`, `stress`, `ramp`. |
| `--warmup` | `-w` | Override the warm-up duration, in minutes. |
| `--out` | `-o` | Output directory for file reports. |

## Custom reports

Pacer ships console, CSV, and HTML writers, but reporting is fully extensible. Implement
`IReportWriter` and register it with `AddReportWriter<T>()` — every registered writer runs after
**every** test run:

```csharp
public sealed class JsonReportWriter : IReportWriter
{
    public string Format => "json";

    public async Task WriteAsync(IReadOnlyList<RunReport> reports, string outputDirectory, CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDirectory);
        foreach (var report in reports)
        {
            var path = Path.Combine(outputDirectory, $"{report.ScenarioName}.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(report), ct);
        }
    }
}

builder.Services.AddPacer()
    .AddScenario(MyScenarios.Checkout())
    .AddReportWriter<JsonReportWriter>();
```

A writer receives the immutable `RunReport`s (per-step `StepStats`, the journey aggregate, interval
snapshots, and environment info) and the output directory.

## Live metrics

While a test runs, watch the `Pacer` meter:

```bash
dotnet-counters monitor --name <your-app> --counters Pacer
```

## Building from source

```bash
dotnet build
dotnet test
dotnet run --project src/Pacer.Sample -- run --scenario checkout --duration 1
```

## License

Apache-2.0.

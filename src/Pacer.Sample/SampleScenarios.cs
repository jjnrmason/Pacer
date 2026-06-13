using Microsoft.Extensions.Logging;
using Pacer.Load;
using Pacer.Scenarios;
using Pacer.Steps;

namespace Pacer.Sample;

/// <summary>Shared data prepared once before the checkout scenario runs.</summary>
public sealed record CheckoutData(string AuthToken, IReadOnlyList<int> ProductIds);

/// <summary>
/// Example scenarios that simulate a small web workload. The steps just sleep for a randomised,
/// realistic amount of time and occasionally fail, so the sample runs anywhere with no external
/// dependencies while still producing meaningful latency and throughput numbers.
/// </summary>
public static class SampleScenarios
{
    /// <summary>A three-step checkout journey with inline one-time setup and a ramp (bell-curve) load.</summary>
    public static Scenario Checkout() => Scenario.Create("checkout")
        .InGroup("storefront")
        .WithSetup(ctx =>
        {
            ctx.Logger.LogInformation("Preparing checkout test data…");
            var products = Enumerable.Range(1000, 50).ToArray();
            return ValueTask.FromResult(new CheckoutData($"token-{Guid.NewGuid():N}", products));
        })
        .WithWarmup(TimeSpan.FromSeconds(2))
        .AddStep("login", async ctx =>
        {
            var data = ctx.ScenarioDataAs<CheckoutData>()!;
            await SimulateWorkAsync(ctx, 5, 15);
            return StepResult.Ok(bytesSent: 128, bytesReceived: 256, payload: data.AuthToken);
        })
        .AddStep("browse", async ctx =>
        {
            var data = ctx.ScenarioDataAs<CheckoutData>()!;
            var product = data.ProductIds[ctx.Random.Next(data.ProductIds.Count)];
            await SimulateWorkAsync(ctx, 10, 35);
            return StepResult.Ok(bytesReceived: 4096, payload: product);
        })
        .AddStep("purchase", async ctx =>
        {
            await SimulateWorkAsync(ctx, 15, 45);
            return ctx.Random.NextDouble() < 0.02
                ? StepResult.Fail(status: "payment-declined")
                : StepResult.Ok(bytesSent: 320, bytesReceived: 512);
        })
        .WithLoad(LoadProfiles.Ramp(peak: 50, rampUp: TimeSpan.FromSeconds(10), hold: TimeSpan.FromSeconds(20), rampDown: TimeSpan.FromSeconds(10)));

    /// <summary>A simpler two-step search scenario sharing the storefront group.</summary>
    public static Scenario Search() => Scenario.Create("search")
        .InGroup("storefront")
        // These steps report transfer via the context byte counters rather than on the result —
        // handy when a step makes several calls, or you want the count recorded even on failure.
        .AddStep("query", async ctx =>
        {
            await SimulateWorkAsync(ctx, 8, 25);
            ctx.AddBytesReceived(8192);
            return StepResult.Ok();
        })
        .AddStep("render", async ctx =>
        {
            await SimulateWorkAsync(ctx, 3, 12);
            ctx.AddBytesReceived(1024);
            return StepResult.Ok();
        })
        .WithLoad(LoadProfiles.Load(users: 25, duration: TimeSpan.FromSeconds(30)));

    private static Task SimulateWorkAsync(IStepContext context, int minMs, int maxMs)
        => Task.Delay(context.Random.Next(minMs, maxMs), context.CancellationToken);
}

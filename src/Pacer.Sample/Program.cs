using Pacer.Hosting;
using Pacer.Sample;

// A Pacer console app: register Pacer and its scenarios, then hand the command-line
// arguments to PacerApplication. Try:
//   dotnet run -- list
//   dotnet run -- run --scenario checkout --out ./artifacts
//   dotnet run -- run --group storefront --profile stress --users 100 --duration 2
return await PacerApplication.RunAsync(args, builder =>
{
    builder.Services.AddPacer()
        .AddScenario(SampleScenarios.Checkout())
        .AddScenario(SampleScenarios.Search());
});

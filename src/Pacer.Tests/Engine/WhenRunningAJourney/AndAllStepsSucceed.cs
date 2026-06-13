using Pacer.Engine;
using Pacer.Steps;

namespace Pacer.Tests.Engine.WhenRunningAJourney;

public partial class WhenRunningAJourney
{
    public class AndAllStepsSucceed : JourneyRunnerTestBase
    {
        [Test]
        public async Task ThenItRecordsEveryStepInOrder()
        {
            var steps = new[] { OkStep("login"), OkStep("browse"), OkStep("purchase") };

            await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            var names = this.Recorder.Records.Select(r => r.Name).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "login", "browse", "purchase" }));
        }

        [Test]
        public async Task ThenItReportsTheJourneyAsSuccessful()
        {
            var steps = new[] { OkStep("login"), OkStep("browse") };

            var outcome = await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            Assert.That(outcome.IsOk, Is.True);
        }

        [Test]
        public async Task ThenItPassesEachStepsPayloadToTheNext()
        {
            object? seen = null;
            var steps = new[] {
                OkStep("login", payload: "token-123"),
                new Step("use-token", (ctx, _) => {
                    seen = ctx.Previous;
                    return ValueTask.FromResult(StepResult.Ok());
                }),
            };

            await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            Assert.That(seen, Is.EqualTo("token-123"));
        }
    }
}
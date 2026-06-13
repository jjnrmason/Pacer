using Pacer.Engine;

namespace Pacer.Tests.Engine.WhenRunningAJourney;

public partial class WhenRunningAJourney
{
    public class AndAStepFails : JourneyRunnerTestBase
    {
        [Test]
        public async Task ThenItStopsThePipelineAtTheFailedStep()
        {
            var steps = new[] { OkStep("login"), FailStep("browse"), OkStep("purchase") };

            await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            var names = this.Recorder.Records.Select(r => r.Name).ToArray();
            Assert.That(names, Is.EqualTo(new[] { "login", "browse" }));
        }

        [Test]
        public async Task ThenItReportsTheJourneyAsFailed()
        {
            var steps = new[] { OkStep("login"), FailStep("browse") };

            var outcome = await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            Assert.That(outcome.IsOk, Is.False);
        }

        [Test]
        public async Task ThenAThrowingStepIsRecordedAsAFailure()
        {
            var steps = new[] { ThrowStep("login") };

            await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            Assert.That(this.Recorder.Records[0].Result.IsOk, Is.False);
        }

        [Test]
        public async Task ThenAThrowingStepStopsThePipeline()
        {
            var steps = new[] { ThrowStep("login"), OkStep("browse") };

            await JourneyRunner.RunAsync(steps, this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            Assert.That(this.Recorder.Records, Has.Count.EqualTo(1));
        }
    }
}
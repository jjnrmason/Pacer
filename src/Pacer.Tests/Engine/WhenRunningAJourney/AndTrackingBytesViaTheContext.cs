using Pacer.Engine;
using Pacer.Steps;

namespace Pacer.Tests.Engine.WhenRunningAJourney;

public partial class WhenRunningAJourney
{
    public class AndTrackingBytesViaTheContext : JourneyRunnerTestBase
    {
        [Test]
        public async Task ThenItFoldsTheCountersIntoTheRecordedStep()
        {
            var step = new Step("call", (ctx, _) =>
            {
                ctx.AddBytesSent(40);
                ctx.AddBytesReceived(100);
                return ValueTask.FromResult(StepResult.Ok());
            });

            await JourneyRunner.RunAsync([step], this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            var recorded = this.Recorder.Records.Single().Result;
            Assert.That(recorded.BytesSent, Is.EqualTo(40));
            Assert.That(recorded.BytesReceived, Is.EqualTo(100));
        }

        [Test]
        public async Task ThenItSumsCounterBytesWithTheResultShorthand()
        {
            var step = new Step("call", (ctx, _) =>
            {
                ctx.AddBytesReceived(100);
                return ValueTask.FromResult(StepResult.Ok(bytesReceived: 50));
            });

            await JourneyRunner.RunAsync([step], this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            Assert.That(this.Recorder.Records.Single().Result.BytesReceived, Is.EqualTo(150));
        }

        [Test]
        public async Task ThenItResetsTheCountersBetweenSteps()
        {
            var first = new Step("a", (ctx, _) =>
            {
                ctx.AddBytesReceived(100);
                return ValueTask.FromResult(StepResult.Ok());
            });
            var second = new Step("b", (ctx, _) =>
            {
                ctx.AddBytesReceived(7);
                return ValueTask.FromResult(StepResult.Ok());
            });

            await JourneyRunner.RunAsync([first, second], this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            Assert.That(this.Recorder.Records[1].Result.BytesReceived, Is.EqualTo(7));
        }

        [Test]
        public async Task ThenItStillCapturesBytesWhenTheStepFails()
        {
            var step = new Step("call", (ctx, _) =>
            {
                ctx.AddBytesSent(40);
                ctx.AddBytesReceived(100);
                return ValueTask.FromResult(StepResult.Fail());
            });

            await JourneyRunner.RunAsync([step], this.Context, this.TimeProvider, this.Recorder, CancellationToken.None);

            var recorded = this.Recorder.Records.Single().Result;
            Assert.That(recorded.IsOk, Is.False);
            Assert.That(recorded.BytesSent, Is.EqualTo(40));
            Assert.That(recorded.BytesReceived, Is.EqualTo(100));
        }

        [Test]
        public void ThenAddingNegativeBytesThrows()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => this.Context.AddBytesReceived(-1));
        }
    }
}

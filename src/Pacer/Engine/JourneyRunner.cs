using Microsoft.Extensions.Logging;
using Pacer.Steps;

namespace Pacer.Engine;

/// <summary>The result of running one journey (a single pass through a scenario's steps).</summary>
internal readonly record struct JourneyOutcome(bool IsOk, TimeSpan Elapsed);

/// <summary>
/// Executes a scenario's steps once, in order, as a pipeline for a single virtual user. Each step
/// is timed and recorded; a failed or throwing step ends the journey early; a successful step's
/// payload is handed to the next step via <see cref="IStepContext.Previous"/>.
/// </summary>
internal static class JourneyRunner
{
    public static async ValueTask<JourneyOutcome> RunAsync(
        IReadOnlyList<Step> steps,
        StepContext context,
        TimeProvider timeProvider,
        IStepRecorder recorder,
        CancellationToken cancellationToken)
    {
        context.Previous = null;
        var journeyStart = timeProvider.GetTimestamp();
        var allOk = true;

        foreach (var step in steps)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                allOk = false;
                break;
            }

            var stepStart = timeProvider.GetTimestamp();
            StepResult result;
            try
            {
                result = await step.Execute(context, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                allOk = false;
                break;
            }
            catch (Exception ex)
            {
                result = StepResult.Fail(ex.GetType().Name);
                context.Logger.LogError(ex, "Step '{Step}' threw an exception.", step.Name);
            }

            var elapsed = timeProvider.GetElapsedTime(stepStart);
            recorder.Record(step.Name, elapsed, result);

            if (!result.IsOk)
            {
                allOk = false;
                break;
            }

            context.Previous = result.Payload;
        }

        return new JourneyOutcome(allOk, timeProvider.GetElapsedTime(journeyStart));
    }
}

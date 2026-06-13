namespace Pacer.Load;

/// <summary>
/// Factory methods for the five built-in closed-model load profiles. Each returns a
/// <see cref="LoadProfile"/> built from <see cref="LoadStage"/>s; callers can also construct a
/// <see cref="LoadProfile"/> directly from custom stages.
/// </summary>
/// <example>
/// <code><![CDATA[
/// // Constant 100 users for 5 minutes.
/// var steady = LoadProfiles.Load(users: 100, duration: TimeSpan.FromMinutes(5));
///
/// // Bell curve: ramp 0->200, hold, ramp back to 0.
/// var ramp = LoadProfiles.Ramp(
///     peak: 200,
///     rampUp: TimeSpan.FromSeconds(30),
///     hold: TimeSpan.FromMinutes(1),
///     rampDown: TimeSpan.FromSeconds(30));
///
/// // Staircase up to the breaking point.
/// var stress = LoadProfiles.Stress(start: 20, max: 200, step: 20, stepDuration: TimeSpan.FromSeconds(15));
///
/// // Custom stages.
/// var custom = new LoadProfile("Custom", [new LoadStage(0, 50, TimeSpan.FromSeconds(10))]);
/// ]]></code>
/// </example>
public static class LoadProfiles
{
    /// <summary>Constant <paramref name="users"/> for the whole <paramref name="duration"/>.</summary>
    public static LoadProfile Load(int users, TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(users);
        return new LoadProfile("Load", [new LoadStage(users, users, duration)]);
    }

    /// <summary>
    /// Endurance test: constant <paramref name="users"/> held for a long <paramref name="duration"/>.
    /// Mechanically identical to <see cref="Load"/> but distinguished in reports.
    /// </summary>
    public static LoadProfile Soak(int users, TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(users);
        return new LoadProfile("Soak", [new LoadStage(users, users, duration)]);
    }

    /// <summary>
    /// A baseline load interrupted by a sudden spike: <paramref name="baseline"/> users, then an
    /// instantaneous jump to <paramref name="spike"/> for <paramref name="spikeDuration"/>, then
    /// back to <paramref name="baseline"/>. The non-spike time is split evenly before and after.
    /// </summary>
    public static LoadProfile Spike(int baseline, int spike, TimeSpan spikeDuration, TimeSpan totalDuration)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseline);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(spike);
        if (spikeDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(spikeDuration), spikeDuration, "Spike duration must be greater than zero.");
        if (spikeDuration >= totalDuration)
            throw new ArgumentOutOfRangeException(nameof(totalDuration), totalDuration, "Total duration must be greater than the spike duration.");

        var rest = totalDuration - spikeDuration;
        var before = rest / 2;
        var after = rest - before;

        return new LoadProfile("Spike",
        [
            new LoadStage(baseline, baseline, before),
            new LoadStage(spike, spike, spikeDuration),
            new LoadStage(baseline, baseline, after),
        ]);
    }

    /// <summary>
    /// A staircase that steps the user count up from <paramref name="start"/> to
    /// <paramref name="max"/> in increments of <paramref name="step"/>, holding each level for
    /// <paramref name="stepDuration"/> — used to find the breaking point.
    /// </summary>
    public static LoadProfile Stress(int start, int max, int step, TimeSpan stepDuration)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(start);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(step);
        if (max < start)
            throw new ArgumentOutOfRangeException(nameof(max), max, "Max users must be greater than or equal to start users.");
        if (stepDuration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(stepDuration), stepDuration, "Step duration must be greater than zero.");

        var stages = new List<LoadStage>();
        for (var users = start; users < max; users += step)
            stages.Add(new LoadStage(users, users, stepDuration));
        stages.Add(new LoadStage(max, max, stepDuration));

        return new LoadProfile("Stress", stages);
    }

    /// <summary>
    /// A bell curve: ramp linearly from zero to <paramref name="peak"/> over
    /// <paramref name="rampUp"/>, optionally hold at the peak for <paramref name="hold"/>, then
    /// ramp back down to zero over <paramref name="rampDown"/>.
    /// </summary>
    public static LoadProfile Ramp(int peak, TimeSpan rampUp, TimeSpan hold, TimeSpan rampDown)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(peak);
        if (rampUp <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(rampUp), rampUp, "Ramp-up duration must be greater than zero.");
        if (rampDown <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(rampDown), rampDown, "Ramp-down duration must be greater than zero.");
        if (hold < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(hold), hold, "Hold duration cannot be negative.");

        var stages = new List<LoadStage>(3) { new(0, peak, rampUp) };
        if (hold > TimeSpan.Zero)
            stages.Add(new LoadStage(peak, peak, hold));
        stages.Add(new LoadStage(peak, 0, rampDown));

        return new LoadProfile("Ramp", stages);
    }
}

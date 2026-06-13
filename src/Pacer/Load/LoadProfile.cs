namespace Pacer.Load;

/// <summary>
/// A closed-model load profile: an ordered list of <see cref="LoadStage"/>s describing how the
/// target number of concurrent virtual users changes over time. The engine samples
/// <see cref="TargetUsersAt"/> as the run progresses and adds or removes virtual users to match.
/// </summary>
public sealed class LoadProfile
{
    /// <summary>The stages that make up the profile, in execution order.</summary>
    public IReadOnlyList<LoadStage> Stages { get; }

    /// <summary>Total wall-clock duration of the profile (sum of all stage durations).</summary>
    public TimeSpan TotalDuration { get; }

    /// <summary>The highest target user count reached across all stages.</summary>
    public int PeakUsers { get; }

    /// <summary>
    /// A short label describing the shape of the profile (e.g. "Load", "Spike", "Ramp").
    /// Used in reports; does not affect scheduling.
    /// </summary>
    public string Kind { get; }

    /// <summary>Creates a load profile from a non-empty list of stages.</summary>
    public LoadProfile(string kind, IReadOnlyList<LoadStage> stages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);
        ArgumentNullException.ThrowIfNull(stages);
        if (stages.Count == 0)
            throw new ArgumentException("A load profile must contain at least one stage.", nameof(stages));

        Kind = kind;
        Stages = stages;

        var total = TimeSpan.Zero;
        var peak = 0;
        foreach (var stage in stages)
        {
            total += stage.Duration;
            peak = Math.Max(peak, stage.PeakUsers);
        }

        TotalDuration = total;
        PeakUsers = peak;
    }

    /// <summary>
    /// Returns the target number of active virtual users at the given elapsed time. Before the
    /// run starts this is the first stage's start count; at or beyond <see cref="TotalDuration"/>
    /// it is the final stage's end count. Within a stage the value is linearly interpolated.
    /// A stage boundary belongs to the following stage, which produces the instantaneous jumps
    /// used by profiles such as Spike.
    /// </summary>
    public int TargetUsersAt(TimeSpan elapsed)
    {
        if (elapsed <= TimeSpan.Zero)
            return Stages[0].StartUsers;
        if (elapsed >= TotalDuration)
            return Stages[^1].EndUsers;

        var accumulated = TimeSpan.Zero;
        foreach (var stage in Stages)
        {
            var stageEnd = accumulated + stage.Duration;
            if (elapsed < stageEnd)
            {
                if (stage.StartUsers == stage.EndUsers)
                    return stage.StartUsers;

                var fraction = (elapsed - accumulated) / stage.Duration;
                var users = stage.StartUsers + (stage.EndUsers - stage.StartUsers) * fraction;
                return (int)Math.Round(users, MidpointRounding.AwayFromZero);
            }

            accumulated = stageEnd;
        }

        return Stages[^1].EndUsers;
    }
}

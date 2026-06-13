namespace Pacer.Load;

/// <summary>
/// A single segment of a <see cref="LoadProfile"/>. The target number of active virtual users
/// is linearly interpolated from <see cref="StartUsers"/> to <see cref="EndUsers"/> across
/// <see cref="Duration"/>. A constant stage has <see cref="StartUsers"/> == <see cref="EndUsers"/>;
/// an instantaneous change in level is expressed as the boundary between two adjacent stages.
/// </summary>
public readonly record struct LoadStage
{
    /// <summary>Active virtual users at the start of the stage.</summary>
    public int StartUsers { get; }

    /// <summary>Active virtual users at the end of the stage.</summary>
    public int EndUsers { get; }

    /// <summary>How long the stage lasts. Must be greater than zero.</summary>
    public TimeSpan Duration { get; }

    /// <summary>Creates a load stage.</summary>
    public LoadStage(int startUsers, int endUsers, TimeSpan duration)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(startUsers);
        ArgumentOutOfRangeException.ThrowIfNegative(endUsers);
        if (duration <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Stage duration must be greater than zero.");

        StartUsers = startUsers;
        EndUsers = endUsers;
        Duration = duration;
    }

    /// <summary>The greater of the stage's start and end user counts.</summary>
    public int PeakUsers => Math.Max(StartUsers, EndUsers);
}

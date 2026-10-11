namespace Roster.Application.Participants;

/// <summary>The game has reached its participant limit: nobody else can join.</summary>
public sealed class ParticipantLimitReachedException : InvalidOperationException
{
    public int Limit { get; }

    public ParticipantLimitReachedException(int limit)
        : base($"The game has reached its limit of {limit} participants.")
    {
        Limit = limit;
    }
}

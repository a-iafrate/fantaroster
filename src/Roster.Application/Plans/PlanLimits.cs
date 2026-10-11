namespace Roster.Application.Plans;

/// <summary>The participant limits of the plans. Set in configuration (Plans:FreeParticipantLimit, Plans:CommunityParticipantLimit).</summary>
public sealed record PlanLimits(int FreeParticipantLimit = 50, int CommunityParticipantLimit = 300);

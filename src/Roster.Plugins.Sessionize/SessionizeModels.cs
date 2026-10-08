using System.Text.Json.Serialization;

namespace Roster.Plugins.Sessionize;

internal sealed record SessionizeAllResponse(
    [property: JsonPropertyName("speakers")] IReadOnlyList<SessionizeSpeaker>? Speakers,
    [property: JsonPropertyName("sessions")] IReadOnlyList<SessionizeSession>? Sessions
);

internal sealed record SessionizeSpeaker(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("firstName")] string? FirstName,
    [property: JsonPropertyName("lastName")] string? LastName,
    [property: JsonPropertyName("fullName")] string? FullName,
    [property: JsonPropertyName("tagLine")] string? TagLine,
    [property: JsonPropertyName("profilePicture")] string? ProfilePicture
);

internal sealed record SessionizeSession(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("title")] string? Title,
    [property: JsonPropertyName("description")] string? Description,
    [property: JsonPropertyName("speakers")] IReadOnlyList<string>? Speakers
);

using Microsoft.AspNetCore.Components.Web;

namespace Roster.Web.Client;

/// <summary>
/// Render mode for participant pages. Prerendering is disabled because the participant
/// token lives in browser storage, which the server cannot read during prerender.
/// </summary>
public static class ParticipantRenderMode
{
    public static readonly InteractiveWebAssemblyRenderMode Instance = new(prerender: false);
}

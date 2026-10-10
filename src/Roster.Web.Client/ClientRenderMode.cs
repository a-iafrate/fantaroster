using Microsoft.AspNetCore.Components.Web;

namespace Roster.Web.Client;

/// <summary>
/// Render mode for the client-side surfaces (participant, referee, big screen). Prerendering is
/// disabled because their tokens live in browser storage, which the server cannot read during prerender.
/// </summary>
public static class ClientRenderMode
{
    public static readonly InteractiveWebAssemblyRenderMode Instance = new(prerender: false);
}

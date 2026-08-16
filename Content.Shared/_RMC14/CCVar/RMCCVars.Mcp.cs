using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.CCVar;

public sealed partial class RMCCVars
{
    /// <summary>
    ///     Enables the embedded MCP (Model Context Protocol) server for mapping agents.
    ///     Only has an effect in builds that compile the MCP server in
    ///     (non-FULL_RELEASE builds, or release builds made with -p:EnableMcp=true).
    /// </summary>
    public static readonly CVarDef<bool> RMCMcpEnabled =
        CVarDef.Create("rmc.mcp.enabled", true, CVar.SERVERONLY);

    /// <summary>
    ///     Bearer token for the MCP endpoint. When empty, only loopback connections are accepted.
    ///     When set, non-loopback connections must present "Authorization: Bearer &lt;token&gt;".
    /// </summary>
    public static readonly CVarDef<string> RMCMcpToken =
        CVarDef.Create("rmc.mcp.token", string.Empty, CVar.SERVERONLY | CVar.CONFIDENTIAL);
}

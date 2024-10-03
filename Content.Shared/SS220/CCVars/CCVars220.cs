using Robust.Shared.Configuration;

namespace Content.Shared.SS220.CCVars;

[CVarDefs]
public sealed class CCVars220
{
    #region Discord Auth

    /// <summary>
    ///     Enabled Discord linking, show linking button and modal window
    /// </summary>
    public static readonly CVarDef<bool> DiscordAuthEnabled =
        CVarDef.Create("discord_auth.enabled", false, CVar.SERVERONLY);

    /// <summary>
    ///     URL of the Discord auth server API
    /// </summary>
    public static readonly CVarDef<string> DiscordAuthApiUrl =
        CVarDef.Create("discord_auth.api_url", "", CVar.SERVERONLY);

    /// <summary>
    ///     Secret key of the Discord auth server API
    /// </summary>
    public static readonly CVarDef<string> DiscordAuthApiKey =
        CVarDef.Create("discord_auth.api_key", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Delay in seconds before first load of the discord sponsors data.
    /// </summary>
    public static readonly CVarDef<float> DiscordSponsorsCacheLoadDelaySeconds =
        CVarDef.Create("discord_sponsors_cache.load_delay_seconds", 10f, CVar.SERVERONLY);

    /// <summary>
    ///     Interval in seconds between refreshes of the discord sponsors data.
    /// </summary>
    public static readonly CVarDef<float> DiscordSponsorsCacheRefreshIntervalSeconds =
        CVarDef.Create("discord_sponsors_cache.refresh_interval_seconds", 60f * 60f * 4f, CVar.SERVERONLY);

    /// <summary>
    ///     Controls whether the server will deny any players that are not whitelisted in the Prime DB.
    /// </summary>
    public static readonly CVarDef<bool> PrimelistEnabled =
        CVarDef.Create("primelist.enabled", false, CVar.SERVERONLY);

    #endregion
}

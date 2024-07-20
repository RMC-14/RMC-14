using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.CCVar;

[CVarDefs]
public sealed class RMCCVars : CVars
{
    public static readonly CVarDef<float> CMXenoDamageDealtMultiplier =
        CVarDef.Create("rmc.xeno_damage_dealt_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMXenoDamageReceivedMultiplier =
        CVarDef.Create("rmc.xeno_damage_received_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMXenoSpeedMultiplier =
        CVarDef.Create("rmc.xeno_speed_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> CMPlayVoicelinesArachnid =
        CVarDef.Create("rmc.play_voicelines_arachnid", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CMPlayVoicelinesDiona =
        CVarDef.Create("rmc.play_voicelines_diona", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CMPlayVoicelinesDwarf =
        CVarDef.Create("rmc.play_voicelines_dwarf", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CMPlayVoicelinesFelinid =
        CVarDef.Create("rmc.play_voicelines_felinid", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CMPlayVoicelinesHuman =
        CVarDef.Create("rmc.play_voicelines_human", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CMPlayVoicelinesMoth =
        CVarDef.Create("rmc.play_voicelines_moth", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CMPlayVoicelinesReptilian =
        CVarDef.Create("rmc.play_voicelines_reptilian", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> CMPlayVoicelinesSlime =
        CVarDef.Create("rmc.play_voicelines_slime", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<string> CMOocWebhook =
        CVarDef.Create("rmc.ooc_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<int> CMMaxHeavyAttackTargets =
        CVarDef.Create("rmc.max_heavy_attack_targets", 3, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMBloodlossMultiplier =
        CVarDef.Create("rmc.bloodloss_multiplier", 1.5f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMBleedTimeMultiplier =
        CVarDef.Create("rmc.bleed_time_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMMarinesPerXeno =
        CVarDef.Create("rmc.marines_per_xeno", 5.5f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCAutoBalance =
        CVarDef.Create("rmc.auto_balance", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCAutoBalanceStep =
        CVarDef.Create("rmc.auto_balance_step", 0.25f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCAutoBalanceMin =
        CVarDef.Create("rmc.auto_balance_min", 3f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCAutoBalanceMax =
        CVarDef.Create("rmc.auto_balance_max", 6f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPatronLobbyMessageTimeSeconds =
        CVarDef.Create("rmc.patron_lobby_message_time_seconds", 30, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPatronLobbyMessageInitialDelaySeconds =
        CVarDef.Create("rmc.patron_lobby_message_initial_delay_seconds", 5, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<string> RMCDiscordAccountLinkingMessageLink =
        CVarDef.Create("rmc.discord_account_linking_message_link", "", CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCRequisitionsStartingBalance =
        CVarDef.Create("rmc.requisitions_starting_balance", 10000, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCRequisitionsBalanceGain =
        CVarDef.Create("rmc.requisitions_balance_gain", 400, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<string> RMCDiscordToken =
        CVarDef.Create("rmc.discord_token", "", CVar.SERVER | CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<ulong> RMCDiscordAdminChatChannel =
        CVarDef.Create("rmc.discord_admin_chat_channel", 0UL, CVar.SERVER | CVar.SERVERONLY | CVar.CONFIDENTIAL);

    /// <summary>
    ///     Comma-separated list of maps to load as the planet in the distress signal gamemode.
    /// </summary>
    public static readonly CVarDef<string> RMCPlanetMaps =
        CVarDef.Create("rmc.planet_maps", "/Maps/_RMC14/lv624.yml,/Maps/_RMC14/solaris.yml,/Maps/_RMC14/prison.yml", CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCDrawStorageIconLabels =
        CVarDef.Create("rmc.draw_storage_icon_labels", true, CVar.REPLICATED | CVar.SERVER);
}

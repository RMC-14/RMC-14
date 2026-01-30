using Robust.Shared;
using Robust.Shared.Configuration;

namespace Content.Shared._RMC14.CCVar;

[CVarDefs]
public sealed partial class RMCCVars : CVars
{
    public static readonly CVarDef<float> CMXenoDamageDealtMultiplier =
        CVarDef.Create("rmc.xeno_damage_dealt_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMXenoDamageReceivedMultiplier =
        CVarDef.Create("rmc.xeno_damage_received_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMXenoSpeedMultiplier =
        CVarDef.Create("rmc.xeno_speed_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCAutoPunctuate =
        CVarDef.Create("rmc.auto_punctuate", false, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCAutoEjectMagazines =
        CVarDef.Create("rmc.auto_eject_magazines", true, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<string> CMOocWebhook =
        CVarDef.Create("rmc.ooc_webhook", "", CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<int> CMMaxHeavyAttackTargets =
        CVarDef.Create("rmc.max_heavy_attack_targets", 1, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMBloodlossMultiplier =
        CVarDef.Create("rmc.bloodloss_multiplier", 1.5f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMBleedTimeMultiplier =
        CVarDef.Create("rmc.bleed_time_multiplier", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> CMMarinesPerXeno =
        CVarDef.Create("rmc.marines_per_xeno", 3f, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<bool> RMCAutoBalance =
        CVarDef.Create("rmc.auto_balance", true, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<float> RMCAutoBalanceStep =
        CVarDef.Create("rmc.auto_balance_step", 1f, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<float> RMCAutoBalanceMin =
        CVarDef.Create("rmc.auto_balance_min", 3f, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<float> RMCAutoBalanceMax =
        CVarDef.Create("rmc.auto_balance_max", 6.5f, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<int> RMCPatronLobbyMessageTimeSeconds =
        CVarDef.Create("rmc.patron_lobby_message_time_seconds", 30, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPatronLobbyMessageInitialDelaySeconds =
        CVarDef.Create("rmc.patron_lobby_message_initial_delay_seconds", 5, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<string> RMCDiscordAccountLinkingMessageLink =
        CVarDef.Create("rmc.discord_account_linking_message_link", "", CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCRequisitionsStartingBalance =
        CVarDef.Create("rmc.requisitions_starting_balance", 0, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCRequisitionsBalanceGain =
        CVarDef.Create("rmc.requisitions_balance_gain", 150, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCRequisitionsStartingDollarsPerMarine =
        CVarDef.Create("rmc.requisitions_starting_dollars_per_marine", 0, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCRequisitionsPointsScale =
        CVarDef.Create("rmc.requisitions_points_scale", 12000, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCRequisitionsFreeCratesXenoDivider =
        CVarDef.Create("rmc.requisitions_free_crates_xeno_divider", 4, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<string> RMCDiscordToken =
        CVarDef.Create("rmc.discord_token", "", CVar.SERVER | CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<long> RMCDiscordAdminChatChannel =
        CVarDef.Create("rmc.discord_admin_chat_channel", 0L, CVar.SERVER | CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<long> RMCDiscordMentorChatChannel =
        CVarDef.Create("rmc.discord_mentor_chat_channel", 0L, CVar.SERVER | CVar.SERVERONLY | CVar.CONFIDENTIAL);

    public static readonly CVarDef<int> RMCPlanetCoordinateVariance =
        CVarDef.Create("rmc.planet_coordinate_variance", 500, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCDrawStorageIconLabels =
        CVarDef.Create("rmc.draw_storage_icon_labels", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCFTLCrashLand =
        CVarDef.Create("rmc.ftl_crash_land", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCDropshipInitialDelayMinutes =
        CVarDef.Create("rmc.dropship_initial_delay_minutes", 15f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCDropshipHijackInitialDelayMinutes =
        CVarDef.Create("rmc.dropship_hijack_initial_delay_minutes", 40, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCLandingZonePrimaryAutoMinutes =
        CVarDef.Create("rmc.landing_zone_primary_auto_minutes", 25f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCLandingZoneMiasmaEnabled =
        CVarDef.Create("rmc.landing_zone_miasma_enabled", false, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCCorrosiveAcidTickDelaySeconds =
        CVarDef.Create("rmc.corrosive_acid_tick_delay_seconds", 10, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<string> RMCCorrosiveAcidDamageType =
        CVarDef.Create("rmc.corrosive_acid_damage_type", "Heat", CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCCorrosiveAcidDamageTimeSeconds =
        CVarDef.Create("rmc.corrosive_acid_damage_time_seconds", 40, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCEvolutionPointsRequireOvipositorMinutes =
        CVarDef.Create("rmc.evolution_points_require_ovipositor_minutes", 5, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCEvolutionPointsAccumulateBeforeMinutes =
        CVarDef.Create("rmc.evolution_points_accumulate_before_minutes", 15, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCAtmosTileEqualize =
        CVarDef.Create("rmc.atmos_tile_equalize", false, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCGasTileOverlayUpdate =
        CVarDef.Create("rmc.gas_tile_overlay_update", false, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCActiveInputMoverEnabled =
        CVarDef.Create("rmc.active_input_mover_enabled", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBioscanInitialDelaySeconds =
        CVarDef.Create("rmc.bioscan_initial_delay_seconds", 300, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBioscanCheckDelaySeconds =
        CVarDef.Create("rmc.bioscan_check_delay_seconds", 60, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBioscanMinimumCooldownSeconds =
        CVarDef.Create("rmc.bioscan_minimum_cooldown_seconds", 300, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBioscanBaseCooldownSeconds =
        CVarDef.Create("rmc.bioscan_base_cooldown_seconds", 1800, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBioscanVariance =
        CVarDef.Create("rmc.bioscan_variance", 2, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCDropshipFabricatorStartingPoints =
        CVarDef.Create("rmc.dropship_fabricator_starting_points", 10000, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCDropshipFabricatorGainEverySeconds =
        CVarDef.Create("rmc.dropship_fabricator_gain_every_seconds", 3.33333f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCDropshipCASDebug =
        CVarDef.Create("rmc.dropship_cas_debug", false, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCDropshipFlyByTimeSeconds =
        CVarDef.Create("rmc.dropship_fly_by_time_seconds", 100, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCDropshipHijackTravelTimeSeconds =
        CVarDef.Create("rmc.dropship_hijack_travel_time_seconds", 180, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCEntitiesLogDelete =
        CVarDef.Create("rmc.entities_log_delete", false, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<bool> RMCPlanetMapVote =
        CVarDef.Create("rmc.planet_map_vote", true, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<int> RMCPlanetMapVoteExcludeLast =
        CVarDef.Create("rmc.planet_map_vote_exclude_last", 2, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<bool> RMCUseCarryoverVoting =
        CVarDef.Create("rmc.planet_map_vote_carryover", true, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<int> RMCTacticalMapAnnounceCooldownSeconds =
        CVarDef.Create("rmc.tactical_map_announce_cooldown_seconds", 240, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<int> RMCTacticalMapLineLimit =
        CVarDef.Create("rmc.tactical_map_line_limit", 1000, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> RMCTacticalMapAdminHistorySize =
        CVarDef.Create("rmc.tactical_map_admin_history_size", 100, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCTacticalMapUpdateEverySeconds =
        CVarDef.Create("rmc.tactical_map_update_every_seconds", 0.5f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCTacticalMapForceUpdateEverySeconds =
        CVarDef.Create("rmc.tactical_map_force_update_every_seconds", 30.0f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> RMCTacticalMapShowAreaLabels =
        CVarDef.Create("rmc.tactical_map_show_area_labels", true, CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    public static readonly CVarDef<bool> RMCGunPrediction =
        CVarDef.Create("rmc.gun_prediction", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> RMCGunPredictionPreventCollision =
        CVarDef.Create("rmc.gun_prediction_prevent_collision", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> RMCGunPredictionLogHits =
        CVarDef.Create("rmc.gun_prediction_log_hits", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCGunPredictionCoordinateDeviation =
        CVarDef.Create("rmc.gun_prediction_coordinate_deviation", 3f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCGunPredictionLowestCoordinateDeviation =
        CVarDef.Create("rmc.gun_prediction_lowest_coordinate_deviation", 3f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCGunPredictionAabbEnlargement =
        CVarDef.Create("rmc.gun_prediction_aabb_enlargement", 1.5f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> RMCJobSlotScaling =
        CVarDef.Create("rmc.job_slot_scaling", true, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCEmoteCooldownSeconds =
        CVarDef.Create("rmc.emote_cooldown_seconds", 20f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCPowerUpdateEverySeconds =
        CVarDef.Create("rmc.power_update_every_seconds", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCPowerLoadMultiplier =
        CVarDef.Create("rmc.power_load_multiplier", 0.01f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCMarinesPerSurvivor =
        CVarDef.Create("rmc.marines_per_survivor", 18, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCSurvivorsMinimum =
        CVarDef.Create("rmc.survivors_minimum", 2, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCSurvivorsMaximum =
        CVarDef.Create("rmc.survivors_maximum", 7, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCSpawnerMaxCorpses =
        CVarDef.Create("rmc.spawner_max_corpses", 100, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCHiveSpreadEarlyMinutes =
        CVarDef.Create("rmc.hive_spread_early_minutes", 0, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCNewPlayerTimeTotalHours =
        CVarDef.Create("rmc.new_player_time_total_hours", 25, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCNewPlayerTimeJobHours =
        CVarDef.Create("rmc.new_player_time_job_hours", 10, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBrandNewPlayerTimeJobHours =
        CVarDef.Create("rmc.brand_new_player_time_job_hours", 1, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCLateJoinsPerBurrowedLarvaEarlyThresholdMinutes =
        CVarDef.Create("rmc.late_joins_per_burrowed_larva_early_threshold_minutes", 15f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCLateJoinsPerBurrowedLarvaEarly =
        CVarDef.Create("rmc.late_joins_per_burrowed_larva_early", 7.5f, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<float> RMCLateJoinsPerBurrowedLarva =
        CVarDef.Create("rmc.late_joins_per_burrowed_larva", 7f, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<float> RMCLateJoinsBurrowedLarvaDeathTime =
        CVarDef.Create("rmc.late_joins_burrowed_larva_death_time", 2.5f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCLateJoinsBurrowedLarvaDeathTimeIgnoreBeforeMinutes =
        CVarDef.Create("rmc.late_joins_burrowed_larva_death_time_ignore_before_minutes", 2.5f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBurrowedLarvaSacrificeTimeMinutes =
        CVarDef.Create("rmc.burrowed_larva_sacrifice_time_minutes", 15, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBurrowedLarvaEvolutionPointsPer =
        CVarDef.Create("rmc.burrowed_larva_evolution_points_per", 250, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimeBronzeMedalTimeHours =
        CVarDef.Create("rmc.playtime_bronze_medal_time_hours", 10, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimeSilverMedalTimeHours =
        CVarDef.Create("rmc.playtime_silver_medal_time_hours", 25, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimeGoldMedalTimeHours =
        CVarDef.Create("rmc.playtime_gold_medal_time_hours", 70, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimePlatinumMedalTimeHours =
        CVarDef.Create("rmc.playtime_platinum_medal_time_hours", 175, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimeRubyMedalTimeHours =
        CVarDef.Create("rmc.playtime_ruby_medal_time_hours", 350, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimeAmethystMedalTimeHours =
        CVarDef.Create("rmc.playtime_amethyst_medal_time_hours", 600, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimeEmeraldMedalTimeHours =
        CVarDef.Create("rmc.playtime_emerald_medal_time_hours", 1000, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimePrismaticMedalTimeHours =
        CVarDef.Create("rmc.playtime_prismatic_medal_time_hours", 1500, CVar.REPLICATED | CVar.SERVER);
    // For the future coder: 2100, 2800, 3600, 4500

    public static readonly CVarDef<int> RMCPlaytimeXenoPrefixThreeTimeHours =
        CVarDef.Create("rmc.playtime_xeno_prefix_three_time_hours", 124, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimeXenoPostfixTimeHours =
        CVarDef.Create("rmc.playtime_xeno_postfix_time_hours", 24, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCPlaytimeXenoPostfixTwoTimeHours =
        CVarDef.Create("rmc.playtime_xeno_postfix_two_time_hours", 300, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCDisconnectedXenoGhostRoleTimeSeconds =
        CVarDef.Create("rmc.disconnected_xeno_ghost_role_time_seconds", 300, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCMarineScalingNormal =
        CVarDef.Create("rmc.marine_scaling_normal", 50f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCMarineScalingBonus =
        CVarDef.Create("rmc.marine_scaling_bonus", 0f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCMentorHelpRateLimitPeriod =
        CVarDef.Create("rmc.mentor_help_rate_limit_period", 2f, CVar.SERVERONLY);

    public static readonly CVarDef<int> RMCMentorHelpRateLimitCount =
        CVarDef.Create("rmc.mentor_help_rate_limit_count", 10, CVar.SERVERONLY);

    public static readonly CVarDef<string> RMCMentorHelpSound =
        CVarDef.Create("rmc.mentor_help_sound", "/Audio/_RMC14/Effects/Admin/mhelp.ogg", CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    public static readonly CVarDef<string> RMCMentorChatSound =
        CVarDef.Create("rmc.mentor_chat_sound", "/Audio/Items/pop.ogg", CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCMentorChatVolume =
        CVarDef.Create("rmc.mentor_help_volume", -5f, CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    public static readonly CVarDef<int> RMCJelliesPerQueen =
        CVarDef.Create("rmc.jellies_per_queen", 5, CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    public static readonly CVarDef<int> RMCCommendationMaxLength =
        CVarDef.Create("rmc.commendation_max_length", 1000, CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    /// <summary>
    /// Whether the no EORG popup is enabled.
    /// </summary>
    public static readonly CVarDef<bool> RMCRoundEndNoEorgPopup =
        CVarDef.Create("game.round_end_eorg_popup_enabled", true, CVar.SERVER | CVar.REPLICATED);

    /// <summary>
    /// Skip the no EORG popup.
    /// </summary>
    public static readonly CVarDef<bool> RMCSkipRoundEndNoEorgPopup =
        CVarDef.Create("game.skip_round_end_eorg_popup", false, CVar.CLIENTONLY | CVar.ARCHIVE);

    /// <summary>
    /// How long to display the EORG popup for.
    /// </summary>
    public static readonly CVarDef<float> RMCRoundEndNoEorgPopupTime =
        CVarDef.Create("game.round_end_eorg_popup_time", 5f, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> RMCXenoEvolveSameCasteCooldownSeconds =
        CVarDef.Create("rmc.xeno_evolve_same_caste_cooldown_seconds", 300, CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    /// <summary>
    ///     Whether or not to show a button that opens the guidebook when a player changes their species,
    ///     explaining the difference between each.
    /// </summary>
    public static readonly CVarDef<bool> GuidebookShowEditorSpeciesButton =
        CVarDef.Create("guidebook.show_editor_species_button", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<bool> RMCEnableSuicide =
        CVarDef.Create("rmc.enable_suicide", false, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> RMCWeedKillerDropshipDelaySeconds =
        CVarDef.Create("rmc.weed_killer_dropship_delay_seconds", 20, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> RMCWeedKillerDisableDurationMinutes =
        CVarDef.Create("rmc.weed_killer_disable_duration_minutes", 8, CVar.SERVER | CVar.REPLICATED);

    public static readonly CVarDef<int> RMCIntelPaperScraps =
        CVarDef.Create("rmc.intel_paper_scraps", 45, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelProgressReports =
        CVarDef.Create("rmc.intel_progress_reports", 15, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelFolders =
        CVarDef.Create("rmc.intel_folders", 30, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelTechnicalManuals =
        CVarDef.Create("rmc.intel_technical_manuals", 10, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelDisks =
        CVarDef.Create("rmc.intel_disks", 30, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelExperimentalDevices =
        CVarDef.Create("rmc.intel_experimental_devices", 15, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelResearchPapers =
        CVarDef.Create("rmc.intel_research_papers", 15, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelVialBoxes =
        CVarDef.Create("rmc.intel_vial_boxes", 20, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCIntelMaxProcessTimeMilliseconds =
        CVarDef.Create("rmc.intel_max_process_time_milliseconds", 2f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCIntelAnnounceEveryMinutes =
        CVarDef.Create("rmc.intel_announce_every_minutes", 15f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelPowerObjectiveWattsRequired =
        CVarDef.Create("rmc.intel_power_objective_watts_required", 300000, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCIntelHumanoidCorpsesMax =
        CVarDef.Create("rmc.intel_humanoid_corpses_max", 48, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCMaxTacmapAlertProcessTimeMilliseconds =
    CVarDef.Create("rmc.tacmap_alert_max_process_time_milliseconds", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCParasiteSpawnInitialDelayMinutes =
        CVarDef.Create("rmc.parasite_spawn_initial_delay_minutes", 15f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCXenoSpawnInitialMuteDurationSeconds =
        CVarDef.Create("rmc.xeno_spawn_initial_mute_duration_seconds", 180f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCXenoEarlyEvoPointBoostBeforeMinutes =
        CVarDef.Create("rmc.evolution_early_evo_point_boost_minutes", 15, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCDamageYourself =
        CVarDef.Create("rmc.damage_yourself", false, CVar.ARCHIVE | CVar.CLIENT | CVar.REPLICATED);

    public static readonly CVarDef<float> RMCOverwatchMaxProcessTimeMilliseconds =
        CVarDef.Create("rmc.overwatch_max_process_time_milliseconds", 1f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCOverwatchConsoleUpdateEverySeconds =
        CVarDef.Create("rmc.overwatch_console_update_every_seconds", 0.5f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    ///     If the amount of resin constructs divided by the amount of buildable tiles in an area is higher than this value, the
    ///     plasma cost of new constructs in the area is increased.
    /// </summary>
    public static readonly CVarDef<float> RMCResinConstructionDensityCostIncreaseThreshold =
        CVarDef.Create("rmc.resin_construction_density_cost_increase_threshold", 0.4f, CVar.REPLICATED | CVar.SERVER);

    /// <summary>
    /// Whether this client uses alternate non-phobia inducing sprites
    /// </summary>
    public static readonly CVarDef<bool> RMCUseAlternateSprites =
        CVarDef.Create("rmc.use_alternate_sprites", false, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<int> RMCSunsetDuration =
        CVarDef.Create("rmc.lighting_sunset_duration", 280, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCSunriseDuration =
        CVarDef.Create("rmc.lighting_sunrise_duration", 280, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCForceEndHijackTimeMinutes =
        CVarDef.Create("rmc.force_hijack_end_time_minutes", 25, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCMovementPenCapSubtract =
        CVarDef.Create("rmc.movement_pen_cap_subtract", 0.8f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCMovementBigXenosCancelMovement =
        CVarDef.Create("rmc.movement_big_xenos_cancel_movement", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCHijackShipWeight =
        CVarDef.Create("rmc.hijack_ship_weight", 0.5f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCMinimumHijackBurrowed =
    CVarDef.Create("rmc.hijack_minimum_burrowed", 5, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCDistressXenosMinimum =
        CVarDef.Create("rmc.distress_xenos_minimum", 4, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> VolumeGainCassettes =
        CVarDef.Create("rmc.volume_gain_cassettes", 0.33f, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<float> VolumeGainHijackSong =
        CVarDef.Create("rmc.volume_gain_hijack_song", 0.5f, CVar.REPLICATED | CVar.CLIENT | CVar.ARCHIVE);

    public static readonly CVarDef<bool> HidePlayerIdentities =
        CVarDef.Create("rmc.hide_player_identities", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCQueenBuildingBoost =
    CVarDef.Create("rmc.queen_building_boost", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCQueenBuildingBoostDurationMinutes =
        CVarDef.Create("rmc.queen_building_boost_duration_minutes", 30, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCQueenBuildingBoostSpeedMultiplier =
        CVarDef.Create("rmc.queen_building_boost_speed_multiplier", 5f / 6f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCQueenBuildingBoostRemoteRange =
        CVarDef.Create("rmc.queen_building_boost_remote_range", 50f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCAutomaticCommanderPromotion =
        CVarDef.Create("rmc.automatic_commander_promotion", true, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCDeadChatEnabled =
        CVarDef.Create("rmc.dead_chat_enabled", true, CVar.SERVER | CVar.NOTIFY | CVar.REPLICATED);

    public static readonly CVarDef<bool> RMCDelayRoundEnd =
        CVarDef.Create("rmc.delay_round_end", false, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<bool> RMCLobbyStartPaused =
        CVarDef.Create("rmc.lobby_start_paused", false, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCChatRepeatHistory =
        CVarDef.Create("rmc.chat_repeat_history", 4, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCChatSquadColorMode =
        CVarDef.Create("rmc.chat_squad_color_mode", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<bool> RMCXenoAbilityPreviews =
        CVarDef.Create("rmc.xeno_ability_previews", true, CVar.CLIENTONLY | CVar.ARCHIVE);

    public static readonly CVarDef<int> RMCLagCompensationMilliseconds =
        CVarDef.Create("rmc.lag_compensation_milliseconds", 750, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<float> RMCLagCompensationMarginTiles =
        CVarDef.Create("rmc.lag_compensation_margin_tiles", 0.25f, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<bool> RMCGhostCanBoo =
        CVarDef.Create("rmc.ghosts_can_boo", true, CVar.SERVER | CVar.SERVERONLY);

    public static readonly CVarDef<int> RMCRoyalResinEveryMinutes =
        CVarDef.Create("rmc.royal_resin_every_minutes", 5, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCCommunicationTowerXenoTakeoverMinutes =
        CVarDef.Create("rmc.communication_tower_xeno_takeover_minutes", 55, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCBoonsLiveMarineRequirement =
        CVarDef.Create("rmc.boons_live_marine_requirement", 12, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCKingVoteCandidateTimeRequirementHours =
        CVarDef.Create("rmc.king_vote_candidate_time_requirement", 50, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCKingHatchingFirstWarningMinutes =
        CVarDef.Create("rmc.king_hatching_first_warning_minutes", 5, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCKingVoteStartTimeSeconds =
        CVarDef.Create("rmc.king_vote_start_time_seconds", 60, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCKingVoteAskCandidatesTimeSeconds =
        CVarDef.Create("rmc.king_vote_ask_candidates_time_seconds", 40, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCKingVoteStartHatchingTimeSeconds =
        CVarDef.Create("rmc.king_vote_start_hatching_time_seconds", 20, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<int> RMCNewResinPreventCollideTimeSeconds =
        CVarDef.Create("rmc.new_resin_prevent_collide_time_seconds", 5, CVar.REPLICATED | CVar.SERVER);

    public static readonly CVarDef<string> RMCChemMasterPresets =
        CVarDef.Create("rmc.chemmaster_presets", "", CVar.CLIENT | CVar.ARCHIVE);
}

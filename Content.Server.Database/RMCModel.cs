using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Content.Shared.Database;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;

namespace Content.Server.Database;

[Table("rmc_discord_accounts")]
public sealed class RMCDiscordAccount
{
    [Key]
    public ulong Id { get; set; }

    public RMCLinkedAccount LinkedAccount { get; set; } = default!;
    public List<RMCLinkedAccountLogs> LinkedAccountLogs { get; set; } = default!;
}

[Table("rmc_linked_accounts")]
public sealed class RMCLinkedAccount
{
    [Key]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public ulong DiscordId { get; set; }

    public RMCDiscordAccount Discord { get; set; } = default!;
}

[Table("rmc_patron_tiers")]
public sealed class RMCPatronTier
{
    [Key]
    public int Id { get; set; }

    public bool ShowOnCredits { get; set; }

    public bool GhostColor { get; set; }

    public bool NamedItems { get; set; }

    public bool Figurines { get; set; }

    public bool LobbyMessage { get; set; }

    public bool RoundEndShoutout { get; set; }

    public string Name { get; set; } = default!;

    public ulong DiscordRole { get; set; }

    public int Priority { get; set; }

    public List<RMCPatron> Patrons { get; set; } = default!;
}

[Table("rmc_patrons")]
[Index(nameof(TierId))]
public sealed class RMCPatron
{
    [Key]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public int TierId { get; set; }

    public RMCPatronTier Tier { get; set; } = default!;
    public int? GhostColor { get; set; } = default!;
    public RMCPatronLobbyMessage? LobbyMessage { get; set; } = default!;
    public RMCPatronRoundEndMarineShoutout? RoundEndMarineShoutout { get; set; } = default!;
    public RMCPatronRoundEndXenoShoutout? RoundEndXenoShoutout { get; set; } = default!;
}

[Table("rmc_linking_codes")]
[Index(nameof(Code))]
public sealed class RMCLinkingCodes
{
    [Key]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public Guid Code { get; set; }

    public DateTime CreationTime { get; set; }
}

[Table("rmc_named_items")]
public sealed class RMCNamedItems
{
    [Key, ForeignKey("Profile")]
    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = default!;

    [StringLength(20)]
    public string? PrimaryGunName { get; set; } = default!;

    [StringLength(20)]
    public string? SidearmName { get; set; } = default!;

    [StringLength(20)]
    public string? HelmetName { get; set; } = default!;

    [StringLength(20)]
    public string? ArmorName { get; set; } = default!;

    [StringLength(20)]
    public string? SentryName { get; set; } = default!;
}

[Table("rmc_linked_accounts_logs")]
[Index(nameof(PlayerId))]
[Index(nameof(DiscordId))]
[Index(nameof(At))]
public sealed class RMCLinkedAccountLogs
{
    [Key]
    public int Id { get; set; }

    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public ulong DiscordId { get; set; }

    public RMCDiscordAccount Discord { get; set; } = default!;

    public DateTime At { get; set; }
}

[Table(("rmc_patron_lobby_messages"))]
public sealed class RMCPatronLobbyMessage
{
    [Key, ForeignKey("Patron")]
    public Guid PatronId { get; set; }

    public RMCPatron Patron { get; set; } = default!;

    [StringLength(500)]
    public string Message { get; set; } = default!;
}

[Table(("rmc_patron_round_end_marine_shoutouts"))]
public sealed class RMCPatronRoundEndMarineShoutout
{
    [Key, ForeignKey("Patron")]
    public Guid PatronId { get; set; }

    public RMCPatron Patron { get; set; } = default!;

    [StringLength(100), Required]
    public string Name { get; set; } = default!;
}

[Table(("rmc_patron_round_end_xeno_shoutouts"))]
public sealed class RMCPatronRoundEndXenoShoutout
{
    [Key, ForeignKey("Patron")]
    public Guid PatronId { get; set; }

    public RMCPatron Patron { get; set; } = default!;

    [StringLength(100), Required]
    public string Name { get; set; } = default!;
}

[Table("rmc_role_timer_excludes")]
[PrimaryKey(nameof(PlayerId), nameof(Tracker))]
public sealed class RMCRoleTimerExclude
{
    [ForeignKey("Player")]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public string Tracker { get; set; } = default!;
}

[Table("rmc_squad_preferences")]
public sealed class RMCSquadPreference
{
    [Key, ForeignKey("Player")]
    public int ProfileId { get; set; }

    public Profile Profile { get; set; } = default!;

    public string? Squad { get; set; } // EntProtoId<SquadTeamComponent>
}

[Table("rmc_commendations")]
public sealed class RMCCommendation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id;

    [ForeignKey("Giver")]
    public Guid GiverId { get; set; }

    public Player Giver { get; set; } = default!;

    [ForeignKey("Receiver")]
    public Guid ReceiverId { get; set; }

    public Player Receiver { get; set; } = default!;

    [ForeignKey("Round")]
    public int RoundId { get; set; }

    public Round Round { get; set; } = default!;

    public string GiverName { get; set; } = string.Empty;

    public string ReceiverName { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;

    public CommendationType Type { get; set; }
}

[Table("rmc_player_stats")]
public sealed class RMCPlayerStats
{
    [Key]
    [ForeignKey("Player")]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public int ParasiteInfects { get; set; }
}

[Table("rmc_player_action_order")]
[PrimaryKey(nameof(PlayerId), nameof(Id))]
public sealed class RMCPlayerActionOrder
{
    [ForeignKey("Player")]
    public Guid PlayerId { get; set; }

    public Player Player { get; set; } = default!;

    public string Id { get; set; } = default!;

    public List<string> Actions { get; set; } = default!;
}

[Table("rmc_chat_bans"), Index(nameof(PlayerId)), Index(nameof(Address))]
public sealed class RMCChatBans
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [ForeignKey(nameof(Round))]
    public int? RoundId { get; set; }
    public Round? Round { get; set; }

    [ForeignKey(nameof(Player))]
    public Guid PlayerId { get; set; }
    public Player Player { get; set; } = default!;
    public NpgsqlInet? Address { get; set; }
    public TypedHwid? HWId { get; set; }

    public ChatType Type { get; set; }

    public DateTime BannedAt { get; set; }

    public DateTime? ExpiresAt { get; set; }

    public string Reason { get; set; } = null!;

    public Guid? BanningAdminId { get; set; }
    public Player? BanningAdmin { get; set; }

    public Guid? UnbanningAdminId { get; set; }
    public Player? UnbanningAdmin { get; set; }
    public DateTime? UnbannedAt { get; set; }

    public Guid? LastEditedById { get; set; }
    public Player? LastEditedBy { get; set; }
    public DateTime? LastEditedAt { get; set; }
}

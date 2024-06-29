using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Content.Server.Database;

[Table("rmc_discord_accounts")]
public sealed class RMCDiscordAccount
{
    [Key]
    public ulong Id { get; set; }

    public RMCLinkedAccount LinkedAccount { get; set; } = default!;
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
}

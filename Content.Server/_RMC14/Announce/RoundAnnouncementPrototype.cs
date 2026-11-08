using Content.Shared._RMC14.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Server.Announcements;

public sealed partial class RoundAnnouncementPrototype : ICMSpecific
{
    [DataField]
    public bool IsCM { get; }
}

using Content.Shared._CM14.Prototypes;

// ReSharper disable CheckNamespace
namespace Content.Server.Announcements;

public sealed partial class RoundAnnouncementPrototype : ICMSpecific
{
    [DataField]
    public bool IsCM { get; }
}

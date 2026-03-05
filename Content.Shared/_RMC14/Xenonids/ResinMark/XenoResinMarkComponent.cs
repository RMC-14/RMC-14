using Robust.Shared.Prototypes;
using System;

namespace Content.Shared._RMC14.Xenonids.ResinMark;

[RegisterComponent]
public sealed partial class XenoResinMarkComponent : Component
{
    [DataField]
    public EntProtoId SelectedPingType = "XenoPingMove";

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(10);

    [DataField]
    public EntProtoId MarkerPrototype = "XenoResinMarkerNub";

    [DataField]
    public TimeSpan PingLifetime = TimeSpan.FromDays(3650);

    [DataField]
    public TimeSpan LastPlacedAt = TimeSpan.MinValue;

    [DataField]
    public bool PlacementEnabled;
}

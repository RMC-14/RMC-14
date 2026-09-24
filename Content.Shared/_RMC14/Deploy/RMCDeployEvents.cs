using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._RMC14.Deploy;

/// <summary>
/// Event to show the deploy area to the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCShowDeployAreaEvent(Box2 box, Color color) : EntityEventArgs
{
    public Box2 Box = box;
    public Color Color = color;
}

/// <summary>
/// Event to hide the deploy area highlight/overlay from the client.
/// </summary>
[Serializable, NetSerializable]
public sealed class RMCHideDeployAreaEvent : EntityEventArgs;

/// <summary>
/// DoAfter event for the deploy process, contains the area being deployed to.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RMCDeployDoAfterEvent : DoAfterEvent
{
    public Box2 Area;
    public RMCDeployDoAfterEvent(Box2 area)
    {
        Area = area;
    }
    public override DoAfterEvent Clone() => new RMCDeployDoAfterEvent(Area);
}

/// <summary>
/// DoAfter event for the collapse (packing up) process of a deployed entity.
/// </summary>
[Serializable, NetSerializable]
public sealed partial class RMCParentalCollapseDoAfterEvent : SimpleDoAfterEvent;

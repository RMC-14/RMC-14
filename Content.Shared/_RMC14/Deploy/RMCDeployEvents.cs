using Robust.Shared.Serialization;
using Content.Shared.DoAfter;

namespace Content.Shared._RMC14.Deploy;

[Serializable, NetSerializable]
public sealed class RMCShowDeployAreaEvent(Box2 box, Color color) : EntityEventArgs
{
    public Box2 Box = box;
    public Color Color = color;
}


[Serializable, NetSerializable]
public sealed class RMCHideDeployAreaEvent : EntityEventArgs { }


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

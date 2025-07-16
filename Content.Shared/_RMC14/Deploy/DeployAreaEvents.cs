using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Maths;
using System.Numerics;

namespace Content.Shared._RMC14.Deploy;

[Serializable, NetSerializable]
public sealed class ShowDeployAreaEvent : EntityEventArgs
{
    public System.Numerics.Vector2 Center;
    public float Width;
    public float Height;
    public Color Color;

    public ShowDeployAreaEvent(System.Numerics.Vector2 center, float width, float height, Color color)
    {
        Center = center;
        Width = width;
        Height = height;
        Color = color;
    }
}

[Serializable, NetSerializable]
public sealed class HideDeployAreaEvent : EntityEventArgs { }

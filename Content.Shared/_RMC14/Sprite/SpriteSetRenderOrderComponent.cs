using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Sprite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSpriteSystem))]
public sealed partial class SpriteSetRenderOrderComponent : Component
{
    [DataField, AutoNetworkedField]
    public int? RenderOrder;

    [DataField, AutoNetworkedField]
    public Vector2? Offset;

    [Serializable, NetSerializable]
    public enum Appearance
    {
        Key,
        Offset,
    }
}

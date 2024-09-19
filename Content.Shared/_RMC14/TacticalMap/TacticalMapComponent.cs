using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.TacticalMap;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(SharedTacticalMapSystem))]
public sealed partial class TacticalMapComponent : Component
{
    [DataField]
    public TimeSpan UpdateEvery = TimeSpan.FromSeconds(1);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.FromSeconds(1);

    [DataField]
    public Dictionary<int, TacticalMapBlip> Marines = new();

    [DataField]
    public Dictionary<int, TacticalMapBlip> LastMarineUpdate = new();

    [DataField]
    public Dictionary<int, TacticalMapBlip> Xenos = new();

    [DataField]
    public Queue<TacticalMapLine> Colors = new();

    [DataField]
    public bool MapDirty;

    [DataRecord]
    [Serializable, NetSerializable]
    public readonly record struct TacticalMapBlip(Vector2i Indices, SpriteSpecifier.Rsi Image, Color Color, bool Undefibbable);
}

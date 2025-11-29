using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Tools;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Barricade.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedBarbedSystem))]
public sealed partial class BarbedComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier ThornsDamage = default!;

    [DataField, AutoNetworkedField]
    public bool IsBarbed;

    [DataField, AutoNetworkedField]
    public int MaxHealthIncrease = 50;

    [DataField]
    public EntProtoId Spawn = "BarbedWire1";

    [DataField]
    public ProtoId<ToolQualityPrototype> RemoveQuality = "Cutting";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? CutSound = new SoundPathSpecifier("/Audio/Items/wirecutter.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.35f),
    };

    [DataField, AutoNetworkedField]
    public SoundSpecifier? BarbSound = new SoundPathSpecifier("/Audio/_RMC14/Items/barbed_wire_movement.ogg")
    {
        Params = AudioParams.Default.WithVariation(0.35f),
    };

    [DataField]
    public TimeSpan WireTime = TimeSpan.FromSeconds(2);

    [DataField]
    public TimeSpan CutTime = TimeSpan.FromSeconds(1);

    [DataField]
    public string FixtureId = "fix1";
}

[NetSerializable, Serializable]
public enum BarbedWireVisualLayers : byte
{
    Wire,
}

[NetSerializable, Serializable]
public enum BarbedWireVisuals : byte
{
    UnWired,
    WiredClosed,
    WiredOpen,
}

[Serializable, NetSerializable]
public sealed partial class BarbedDoAfterEvent : SimpleDoAfterEvent
{

}

[Serializable, NetSerializable]
public sealed partial class CutBarbedDoAfterEvent : SimpleDoAfterEvent
{

}

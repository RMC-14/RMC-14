using Content.Shared.DoAfter;
using Content.Shared.FixedPoint;
using Content.Shared._RMC14.Marines.Skills;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHyposprayComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string SlotId = string.Empty;

    [DataField, AutoNetworkedField]
    public string VialName = "beaker";

    [DataField, AutoNetworkedField]
    public string SolutionName = "vial";

    [DataField]
    public SoundSpecifier InjectSound = new SoundPathSpecifier("/Audio/_RMC14/Medical/hypospray.ogg");

    // Syringe stuff below - due to vials assume this only injects

    [DataField]
    public bool OnlyAffectsMobs = true;

    [DataField]
    public FixedPoint2[] TransferAmounts = [FixedPoint2.New(3), FixedPoint2.New(5), FixedPoint2.New(10), FixedPoint2.New(15), FixedPoint2.New(30)];

    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    // Doafter stuff

    [DataField]
    public TimeSpan TacticalReloadTime = TimeSpan.FromSeconds(1.25);

    [DataField, AutoNetworkedField]
    public SkillWhitelist TacticalSkills;

    [DataField]
    public bool NeedHand = true;

    [DataField]
    public bool BreakOnHandChange = true;

    [DataField]
    public float MovementThreshold = 0.1f;
}

[Serializable, NetSerializable]
public sealed partial class TacticalReloadHyposprayDoAfterEvent : SimpleDoAfterEvent
{
}


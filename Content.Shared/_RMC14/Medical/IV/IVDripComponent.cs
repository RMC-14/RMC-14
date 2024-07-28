using Content.Shared._RMC14.Marines.Skills;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Medical.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class IVDripComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? AttachedTo;

    [DataField, AutoNetworkedField]
    public string Slot = "pack";

    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    [DataField, AutoNetworkedField]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(3);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan TransferAt;

    [DataField, AutoNetworkedField]
    public string AttachedState = "hooked";

    [DataField, AutoNetworkedField]
    public string UnattachedState = "unhooked";

    /// <summary>
    ///     Percentages are from 0 to 100
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<(int Percentage, string State)> ReagentStates = new();

    [DataField, AutoNetworkedField]
    public Color FillColor;

    /// <summary>
    ///     From 0 to 100
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FillPercentage;

    [DataField, AutoNetworkedField]
    public int Range = 2;

    [DataField]
    public DamageSpecifier? RipDamage;

    [DataField, AutoNetworkedField]
    public bool Injecting = true;

    [DataField, AutoNetworkedField]
    public ProtoId<EmotePrototype> RipEmote = "Scream";

    [DataField, AutoNetworkedField]
    public Dictionary<EntProtoId<SkillDefinitionComponent>, int> SkillRequired = new() { ["RMCSkillSurgery"] = 1 };
}

[Serializable, NetSerializable]
public enum IVDripVisualLayers
{
    Base,
    Reagent
}

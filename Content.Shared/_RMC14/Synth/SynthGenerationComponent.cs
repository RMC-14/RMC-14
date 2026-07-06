using Content.Shared.Damage.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Synth;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SynthGenerationComponent : Component
{
    /// <summary>
    /// I.E. 1st generation, 3rd generation.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntProtoId<SynthGenerationComponent>? Generation;

    [DataField, AutoNetworkedField]
    public EntProtoId GenerationAction = "ActionChooseGen";

    [DataField, AutoNetworkedField]
    public EntityUid? SelectGenerationActionEntity;

    [DataField]
    public ProtoId<DamageModifierSetPrototype>? DamageModifier;
}

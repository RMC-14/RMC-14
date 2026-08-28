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
    public EntProtoId? Generation;

    [DataField, AutoNetworkedField]
    public EntProtoId GenerationAction = "ActionChooseGen";

    [DataField, AutoNetworkedField]
    public EntityUid? SelectGenerationActionEntity;

    [DataField]
    public ProtoId<DamageModifierSetPrototype>? DamageModifier;

    [DataField]
    public int Priority;

    /// <summary>
    /// Short description shown in the generation selection dialog.
    /// </summary>
    [DataField]
    public string Description = string.Empty;

    /// <summary>
    /// Sets choices, else it defaults to Gens 1,2,3
    /// </summary>
    [DataField]
    public List<EntProtoId<SynthGenerationComponent>> AvailableGenerations = new()
    {
        "RMCSynthGenOne",
        "RMCSynthGenTwo",
        "RMCSynthGenThree"
    };
}

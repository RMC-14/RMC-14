using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCGiantLizardComponent : Component
{
    [DataField]
    public EntProtoId<WorldTargetActionComponent> PounceAction = "RMCActionGiantLizardPounce";

    [ViewVariables]
    public EntityUid? PounceActionEntity;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);

    [ViewVariables]
    public TimeSpan NextUpdateAt;
}

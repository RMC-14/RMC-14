using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Foldable;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCFoldableGunComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Fired = false;

    [DataField, AutoNetworkedField]
    public bool OnActivate = false;

    [DataField, AutoNetworkedField]
    public TimeSpan FoldDelay = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public LocId FoldText;

    [DataField, AutoNetworkedField]
    public LocId FoldTextOthers;

    [DataField, AutoNetworkedField]
    public LocId FinishText;

    [DataField, AutoNetworkedField]
    public LocId FinishTextOthers;

    [DataField, AutoNetworkedField]
    public LocId ExamineText = "rmc-gun-foldable-launcher-examine";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? ToggleFoldSound;

    [DataField, AutoNetworkedField]
    public EntProtoId FoldedEntity;
}

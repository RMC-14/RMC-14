using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Intel;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(IntelSystem))]
public sealed partial class ViewIntelObjectivesComponent : Component
{
    [DataField, AutoNetworkedField]
    public string ActionId = "RMCActionViewIntelObjectives";

    [DataField, AutoNetworkedField]
    public EntityUid? Action;

    [DataField, AutoNetworkedField]
    public IntelTechTree Tree = new();
}

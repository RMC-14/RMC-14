using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Actions;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoActionsSystem))]
public sealed partial class XenoOffensiveActionComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool CanHitBarricades;

    [DataField, AutoNetworkedField]
    public bool CanHitWindows;
}

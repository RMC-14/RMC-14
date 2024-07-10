using Content.Shared._RMC14.Attachable.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Attachable.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AttachableToggleableSystem))]
public sealed partial class AttachableGunPreventShootComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool PreventShoot;

    [DataField, AutoNetworkedField]
    public string Message = "";
}

using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Autodoc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAutodocSystem))]
public sealed partial class OutsideAutodocComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Autodoc;
}

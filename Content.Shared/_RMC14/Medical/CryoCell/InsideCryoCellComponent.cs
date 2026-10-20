using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.CryoCell;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedCryoCellSystem))]
public sealed partial class InsideCryoCellComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? Chamber;
}

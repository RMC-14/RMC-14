using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Evolution;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoNewlyEvolvedComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool TriedClimb;

    [DataField, AutoNetworkedField]
    public List<EntityUid> StopCollide = new();
}

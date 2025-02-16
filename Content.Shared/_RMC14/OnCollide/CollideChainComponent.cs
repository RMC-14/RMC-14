using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.OnCollide;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedOnCollideSystem))]
public sealed partial class CollideChainComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Hit = new();
}

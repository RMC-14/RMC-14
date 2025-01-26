using Content.Shared.Physics;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class ExtinguishFireComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Extinguished;

    [DataField, AutoNetworkedField]
    public CollisionGroup Collision = CollisionGroup.MobLayer | CollisionGroup.MobMask;
}

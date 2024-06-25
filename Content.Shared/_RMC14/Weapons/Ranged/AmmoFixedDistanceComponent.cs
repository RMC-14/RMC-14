using Content.Shared.Physics;
using Robust.Shared.GameStates;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMGunSystem))]
public sealed partial class AmmoFixedDistanceComponent : Component; // TODO: Make it so weapons with this component can have arc fire disabled.

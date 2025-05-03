using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hook;

[RegisterComponent, NetworkedComponent]
public sealed partial class XenoHookOnHitComponent : Component;

//TODO: On a projectile. TailSeize spawns this and hooks it
//Then when the projectile hits something it hooks it
//And tail seize pulls it in
//Simple

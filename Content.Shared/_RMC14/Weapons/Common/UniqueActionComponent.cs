using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Common;

[RegisterComponent, NetworkedComponent]
[Access(typeof(UniqueActionSystem))]
public sealed partial class UniqueActionComponent : Component;

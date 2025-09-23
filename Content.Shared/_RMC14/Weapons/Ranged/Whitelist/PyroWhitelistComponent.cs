using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Whitelist;

[RegisterComponent, NetworkedComponent]
[Access(typeof(CMGunSystem))]
[SpecialistSkillComponent("Pyro")]
public sealed partial class PyroWhitelistComponent : Component;

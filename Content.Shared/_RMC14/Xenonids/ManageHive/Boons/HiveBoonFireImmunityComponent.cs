using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.ManageHive.Boons;

[RegisterComponent, NetworkedComponent]
[Access(typeof(HiveBoonSystem))]
public sealed partial class HiveBoonFireImmunityComponent : Component;

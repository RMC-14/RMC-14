using Content.Shared._RMC14.Xenonids.ManageHive;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ManageHiveSystem))]
public sealed partial class HiveConstructionSuppressAnnouncementsComponent : Component;

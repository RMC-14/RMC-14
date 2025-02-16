using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Actions;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoActionsSystem))]
public sealed partial class XenoOffensiveActionComponent : Component;

using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Construction.ResinWhisper;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ResinWhispererSystem))]
public sealed partial class ResinDoorComponent : Component;

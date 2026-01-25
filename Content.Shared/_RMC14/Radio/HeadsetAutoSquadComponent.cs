using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Radio;

[RegisterComponent, NetworkedComponent]
[Access(typeof(RMCRadioSystem))]
public sealed partial class HeadsetAutoSquadComponent : Component;

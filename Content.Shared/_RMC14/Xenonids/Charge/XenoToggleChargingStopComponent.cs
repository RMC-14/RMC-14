using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoToggleChargingSystem))]
public sealed partial class XenoToggleChargingStopComponent : Component;

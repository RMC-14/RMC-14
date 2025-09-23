using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Charge;

[RegisterComponent, NetworkedComponent]
[Access(typeof(XenoChargeSystem))]
public sealed partial class XenoToggleChargingStopComponent : Component;

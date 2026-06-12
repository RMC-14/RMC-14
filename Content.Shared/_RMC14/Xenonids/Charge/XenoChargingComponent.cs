namespace Content.Shared._RMC14.Xenonids.Charge;


/// <summary>
/// Marker component added to a xeno while it is actively charging (after the DoAfter completes).
/// Used to prevent XenoClawsSystem from overwriting charge damage with melee slash damage values on walls.
/// </summary>
[RegisterComponent]
public sealed partial class XenoChargingComponent : Component;

using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Medical.HUD.Components;

/// <summary>
/// A component that allows the entity to change the holocard component via examination
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class HolocardScannerComponent : Component;

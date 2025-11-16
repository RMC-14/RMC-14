using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Atmos.EntitySystems;
using Content.Server._Forge.Temperature; // Forge - allow temperature controller to access map atmosphere

namespace Content.Server.Atmos.Components;

/// <summary>
///     Component that defines the default GasMixture for a map.
/// </summary>
[RegisterComponent, Access(typeof(SharedAtmosphereSystem), typeof(TemperatureControllerSystem))] // Forge - allow temp controller writes
public sealed partial class MapAtmosphereComponent : SharedMapAtmosphereComponent
{
    /// <summary>
    ///     The default GasMixture a map will have. Space mixture by default.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public GasMixture Mixture = GasMixture.SpaceGas;

    /// <summary>
    ///     Whether empty tiles will be considered space or not.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool Space = true;

    public SharedGasTileOverlaySystem.GasOverlayData Overlay;
}

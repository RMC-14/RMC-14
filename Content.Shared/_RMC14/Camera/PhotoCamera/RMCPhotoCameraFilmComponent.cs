using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Camera.PhotoCamera;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCPhotoCameraFilmComponent : Component
{
    /// <summary>
    ///     Total number of photos this film cartridge provides when loaded into a camera.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int PhotoCharges = 10;
}

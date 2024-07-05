using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Screech;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
[Access(typeof(XenoScreechSystem))]
public sealed partial class RMCXenoScreechShockWaveComponent : Component
{
    /// <summary>
    ///   The speed of each individual wave from the center axis.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float WaveSpeed = 15.3f;

    /// <summary>
    ///     The size of each wave in its width and distortion effect
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float WaveStrength = 1.0f;

    /// <summary>
    ///     The scale of the effect, lower number means a larger total area while smaller numbers downscale it and reduce the effected area.
    /// </summary>
    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public float DownScale = 1.5f;
}


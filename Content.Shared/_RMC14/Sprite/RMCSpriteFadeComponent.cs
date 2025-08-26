using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sprite;

/// <summary>
/// If your client entity is behind an entity with this component, its alpha will be reduced to make the entity visible.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCSpriteFadeComponent : Component
{
    /// <summary>
    /// Target alpha at fade (0.0 = fully transparent, 1.0 = fully visible)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float TargetAlpha = 0.4f;

    /// <summary>
    /// Rate of alpha change per second (the higher the value the faster)
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ChangeRate = 1.0f;

    /// <summary>
    /// If true - react to mouse, if false - not (for example, tent roof)
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ReactToMouse = true;

    /// <summary>
    /// List of layer keys to fade. If empty - fade entire sprite, if not empty - fade only specified layers
    /// </summary>
    [DataField, AutoNetworkedField]
    public List<string> FadeLayers = new();
}

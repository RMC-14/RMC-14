using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Sprite;

/// <summary>
/// Used for showing different sprites to clients due to some sprites causing trypophobia etc
/// </summary>
///
[RegisterComponent, NetworkedComponent]
public sealed partial class RMCAlternateSpriteComponent : Component
{
    [DataField]
    public string NormalSprite;

    [DataField]
    public string AlternateSprite;
}

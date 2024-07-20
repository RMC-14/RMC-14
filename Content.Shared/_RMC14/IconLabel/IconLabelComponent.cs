using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.IconLabel;

/// <summary>
/// A text icon label on a layer above the rest of the icon
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class IconLabelComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId? LabelTextLocId;

    [DataField, AutoNetworkedField]
    public int TextSize = 1;

    [DataField, AutoNetworkedField]
    public String TextColor = "Black";

    [DataField, AutoNetworkedField]
    public Vector2i StoredOffset = new(0, 0);
}

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

    /// <summary>
    /// Localization parameters should be used if the label is not certain at compile-time
    /// </summary>
    [AutoNetworkedField]
    public List<(string, object)> LabelTextParams = new();

    [DataField, AutoNetworkedField]
    public int TextSize = 1;

    [DataField, AutoNetworkedField]
    public String TextColor = "Black";

    [DataField, AutoNetworkedField]
    public Vector2i StoredOffset = new(0, 0);

    [DataField, AutoNetworkedField]
    public int LabelMaxSize = 2;
}

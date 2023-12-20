using Robust.Shared.GameStates;

namespace Content.Shared._CM14.Marines.Medical;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class BloodBagComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Solution = "bag";

    /// <summary>
    ///     From 0 to 100
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FillPercentage;

    [DataField, AutoNetworkedField]
    public Color FillColor;
}

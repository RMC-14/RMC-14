using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Medical.IV;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class BloodPackComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Solution = "pack";

    [DataField, AutoNetworkedField]
    public FixedPoint2 FillPercentage;

    [DataField, AutoNetworkedField]
    public Color FillColor;

    [DataField, AutoNetworkedField]
    public int MaxFillLevels = 7;

    [DataField, AutoNetworkedField]
    public string FillBaseName = "bloodpack";
}

[Serializable, NetSerializable]
public enum BloodPackVisuals
{
    Label,
    Fill
}

using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._CM14.Marines.Medical;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class IVDripComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid AttachedTo;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string Slot = "bag";

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string Solution = "IVBagContents";

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan TransferDelay = TimeSpan.FromSeconds(3);

    [DataField, AutoNetworkedField]
    public TimeSpan TransferAt;

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string AttachedState = "hooked";

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public string UnattachedState = "unhooked";

    [DataField, AutoNetworkedField, ViewVariables(VVAccess.ReadWrite)]
    public List<(int, string)> ReagentStates = new();

    /// <summary>
    ///     From 0 to 100
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FillPercentage;

    [DataField, AutoNetworkedField]
    public Color FillColor;
}

[Serializable, NetSerializable]
public enum IVDripVisualLayers
{
    Base,
    Reagent
}

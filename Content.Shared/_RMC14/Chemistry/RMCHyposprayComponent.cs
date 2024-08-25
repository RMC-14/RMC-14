using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Chemistry;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCHyposprayComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string SlotId = string.Empty;

    [DataField, AutoNetworkedField]
    public string VialName = "beaker";

    [DataField, AutoNetworkedField]
    public string BaseName = "hypospray";

    [DataField, AutoNetworkedField]
    public string SolutionName = "vial";

    //Syringe stuff below

    [DataField]
    public bool OnlyAffectsMobs;

    [DataField]
    public FixedPoint2 MinimumTransferAmount = FixedPoint2.New(5);

    [DataField]
    public FixedPoint2 MaximumTransferAmount = FixedPoint2.New(30);

    [DataField, AutoNetworkedField]
    public FixedPoint2 TransferAmount = FixedPoint2.New(5);
}

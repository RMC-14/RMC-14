using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Inhands;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoInhandsComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public string Prefix;

    [DataField, AutoNetworkedField]
    public string Downed = "downed";

    [DataField, AutoNetworkedField]
    public string Resting = "rest";

	[DataField, AutoNetworkedField]
	public string Ovi = "ovi";
}

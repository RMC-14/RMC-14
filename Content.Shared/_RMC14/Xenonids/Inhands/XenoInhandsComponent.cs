using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Inhands;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class XenoInhandsComponent : Component, IComponentDebug
{
    [DataField(required: true), AutoNetworkedField]
    public string Prefix;

    [DataField, AutoNetworkedField]
    public string Downed = "downed";

    [DataField, AutoNetworkedField]
    public string Resting = "rest";

    [DataField, AutoNetworkedField]
    public string Ovi = "ovi";

    public string GetDebugString()
    {
        return $"Prefix: {Prefix}\r\nDowned: {Downed}\r\nResting: {Resting}\r\nOvi: {Ovi}";
    }
}

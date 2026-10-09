using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Marines.Command;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CommandingOfficerAffiliationComponent : Component
{
    [DataField, AutoNetworkedField]
    public CommandingOfficerAffiliation Affiliation = CommandingOfficerAffiliation.Unaligned;
}

[Serializable, NetSerializable]
public enum CommandingOfficerAffiliation
{
    Unaligned,
    Hawks,
    Doves,
    Magpies,
}

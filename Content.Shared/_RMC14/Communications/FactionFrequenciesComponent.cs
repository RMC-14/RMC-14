using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Communications;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(CommunicationsTowerSystem))]
public sealed partial class FactionFrequenciesComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();
}

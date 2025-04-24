using Content.Shared.Radio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Radio;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCRadioSystem))]
public sealed partial class RMCHeadsetComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<ProtoId<RadioChannelPrototype>> Channels = new();

    /// <summary>
    ///     Determines how much larger the radio message size will be.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? RadioTextIncrease { get; set; } = 0;
}

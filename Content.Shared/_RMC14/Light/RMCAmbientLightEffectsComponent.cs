using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Light;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCAmbientLightEffectsComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<DatasetPrototype> Sunset = "RMCColorSequenceSunsetWarm";

    [DataField, AutoNetworkedField]
    public ProtoId<DatasetPrototype> Sunrise = "RMCColorSequenceSunrise";
}

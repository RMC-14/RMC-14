using Content.Shared.Dataset;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Light;

[RegisterComponent, NetworkedComponent]
public sealed partial class RMCAmbientLightEffectsComponent : Component
{
    [DataField]
    public ProtoId<DatasetPrototype> Sunset = "WarmSunsetColorProgression";

    [DataField]
    public ProtoId<DatasetPrototype> Sunrise = "WarmSunriseColorProgression";
}

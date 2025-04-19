using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Atmos;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlammableSystem))]
public sealed partial class TileFireOnTriggerComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Range = 2;

    [DataField, AutoNetworkedField]
    public EntProtoId Spawn = "RMCTileFire";

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_RMC14/Effects/hit_on_shattered_glass.ogg");

    [DataField, AutoNetworkedField]
    public int? Intensity;

    [DataField, AutoNetworkedField]
    public int? Duration;
}

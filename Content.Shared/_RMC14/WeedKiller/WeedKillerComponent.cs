using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.WeedKiller;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(WeedKillerSystem))]
public sealed partial class WeedKillerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Deployed;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan DeployAt;

    [DataField, AutoNetworkedField]
    public bool Disabled;

    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan DisableAt;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound = new SoundPathSpecifier("/Audio/_RMC14/Effects/rocketpod_fire.ogg");

    [DataField, AutoNetworkedField]
    public EntityUid? Dropship;

    [DataField, AutoNetworkedField]
    public HashSet<EntProtoId> AreaPrototypes = new();

    [DataField]
    public List<EntityUid> LinkedAreas = new();

    [DataField]
    public List<(Entity<MapGridComponent> Grid, Vector2i Indices)> Positions = new();
}

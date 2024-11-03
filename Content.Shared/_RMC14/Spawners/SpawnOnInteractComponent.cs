using Content.Shared.Whitelist;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Spawners;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(RMCSpawnerSystem))]
public sealed partial class SpawnOnInteractComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public EntProtoId Spawn;

    [DataField, AutoNetworkedField]
    public bool RequireEvacuation;

    [DataField, AutoNetworkedField]
    public EntityWhitelist? Blacklist;

    [DataField, AutoNetworkedField]
    public LocId? EvacuationPopup;

    [DataField, AutoNetworkedField]
    public LocId? Popup;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? Sound;
}

using Content.Shared.Storage;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCUnfoldCardboardComponent : Component
{
    [DataField, AutoNetworkedField]
    public LocId VerbText = "rmc-unfold-cardboard-component-verb";

    [DataField, AutoNetworkedField]
    public LocId FailedNotEmptyText = "rmc-unfold-cardboard-component-failed-not-empty";

    [DataField, AutoNetworkedField]
    public List<EntitySpawnEntry> Spawns = new();
}

using Content.Shared.Storage;

namespace Content.Shared._RMC14.Construction;

[RegisterComponent]
public sealed partial class RMCUnfoldCardboardComponent : Component
{
    [DataField]
    public LocId VerbText = "rmc-unfold-cardboard-component-verb";

    [DataField]
    public LocId FailedNotEmptyText = "rmc-unfold-cardboard-component-failed-not-empty";

    [DataField]
    public List<EntitySpawnEntry> Spawns = new();
}

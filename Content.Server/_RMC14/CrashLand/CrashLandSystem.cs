using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.CrashLand;

namespace Content.Server._RMC14.CrashLand;

public sealed class CrashLandSystem : SharedCrashLandSystem
{
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityStorageComponent, CrashLandStartedEvent>(OnCrashLandStarted);
        SubscribeLocalEvent<EntityStorageComponent, CrashLandedEvent>(OnCrashLanded);
    }

    private void OnCrashLandStarted(Entity<EntityStorageComponent> ent, ref CrashLandStartedEvent args)
    {
        ent.Comp.OpenOnMove = false;
        Dirty(ent);
    }

    private void OnCrashLanded(Entity<EntityStorageComponent> ent, ref CrashLandedEvent args)
    {
        if (!args.ShouldDamage)
            return;

        foreach (var entity in ent.Comp.Contents.ContainedEntities)
        {
            ApplyFallingDamage(entity);
        }

        ent.Comp.OpenOnMove = true;
        Dirty(ent);

        _entityStorage.OpenStorage(ent);
    }
}

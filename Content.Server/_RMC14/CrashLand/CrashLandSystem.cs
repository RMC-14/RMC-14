using Content.Server.Storage.Components;
using Content.Server.Storage.EntitySystems;
using Content.Shared._RMC14.CrashLand;
using Content.Shared.ParaDrop;
using Robust.Server.Audio;

namespace Content.Server._RMC14.CrashLand;

public sealed class CrashLandSystem : SharedCrashLandSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
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

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var crashLandingQuery = EntityQueryEnumerator<CrashLandableComponent, CrashLandingComponent>();

        while (crashLandingQuery.MoveNext(out var uid, out var crashLandable, out var crashLanding))
        {
            if (!HasComp<SkyFallingComponent>(uid))
            {
                crashLanding.RemainingTime -= frameTime;
                Dirty(uid, crashLanding);
            }

            if (!(crashLanding.RemainingTime <= 0))
                continue;

            if (crashLanding.DoDamage)
                ApplyFallingDamage(uid);

            var ev = new CrashLandedEvent(crashLanding.DoDamage);
            RaiseLocalEvent(uid, ref ev);

            _audio.PlayPvs(crashLandable.CrashSound, uid);
            RemComp<CrashLandingComponent>(uid);
            Blocker.UpdateCanMove(uid);

        }
    }
}

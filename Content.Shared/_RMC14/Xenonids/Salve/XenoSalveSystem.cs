using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Rest;
using Content.Shared.Mobs;
using Content.Shared.StatusEffect;
using Content.Shared.Stunnable;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Salve;

public sealed class XenoSalveSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    public override void Initialize()
    {
        SubscribeLocalEvent<RecentlySalvedComponent, ComponentStartup>(OnSalveAdded);
        SubscribeLocalEvent<RecentlySalvedComponent, ComponentShutdown>(OnSalveRemoved);
        SubscribeLocalEvent<RecentlySalvedComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RecentlySalvedComponent, XenoRestEvent>(OnVisualsRest);
        SubscribeLocalEvent<RecentlySalvedComponent, KnockedDownEvent>(OnVisualsKnockedDown);
        SubscribeLocalEvent<RecentlySalvedComponent, StatusEffectEndedEvent>(OnVisualsStatusEffectEnded);
    }

    private void OnSalveAdded(Entity<RecentlySalvedComponent> xeno, ref ComponentStartup args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoHealerVisuals.Gooped, true);
    }

    private void OnSalveRemoved(Entity<RecentlySalvedComponent> xeno, ref ComponentShutdown args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoHealerVisuals.Gooped, false);
    }

    private void OnMobStateChanged(Entity<RecentlySalvedComponent> xeno, ref MobStateChangedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoHealerVisuals.Downed, args.NewMobState != MobState.Alive);
    }

    private void OnVisualsRest(Entity<RecentlySalvedComponent> xeno, ref XenoRestEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoHealerVisuals.Resting, args.Resting);
    }

    private void OnVisualsKnockedDown(Entity<RecentlySalvedComponent> xeno, ref KnockedDownEvent args)
    {
        if (_timing.ApplyingState)
            return;

        _appearance.SetData(xeno, XenoHealerVisuals.Downed, true);
    }

    private void OnVisualsStatusEffectEnded(Entity<RecentlySalvedComponent> xeno, ref StatusEffectEndedEvent args)
    {
        if (_timing.ApplyingState)
            return;

        if (args.Key == "KnockedDown")
            _appearance.SetData(xeno, XenoHealerVisuals.Downed, false);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var query = EntityQueryEnumerator<RecentlySalvedComponent>();

        while (query.MoveNext(out var uid, out var salve))
        {
            if (time < salve.ExpiresAt)
                continue;

            RemCompDeferred<RecentlySalvedComponent>(uid);
        }
    }
}

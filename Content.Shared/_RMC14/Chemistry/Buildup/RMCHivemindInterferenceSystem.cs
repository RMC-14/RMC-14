using Content.Shared._RMC14.Xenonids;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Chemistry.Buildup;

public sealed class RMCHivemindInterferenceSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);
    private TimeSpan _nextUpdate;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCHivemindInterferenceComponent, XenoDebuffCleanseEvent>(OnCleanse);
        SubscribeLocalEvent<RMCHivemindInterferenceComponent, RejuvenateEvent>(OnRejuvenate);
    }

    private void OnCleanse(Entity<RMCHivemindInterferenceComponent> ent, ref XenoDebuffCleanseEvent args)
    {
        Cleanse(ent);
    }

    private void OnRejuvenate(Entity<RMCHivemindInterferenceComponent> ent, ref RejuvenateEvent args)
    {
        Cleanse(ent);
    }

    private void Cleanse(EntityUid uid)
    {
        if (_net.IsClient)
            return;

        RemCompDeferred<RMCHivemindInterferenceComponent>(uid);
        EnsureComp<RMCHivemindInterferenceImmuneComponent>(uid);
    }

    public void AddBuildup(EntityUid uid, float magnitude, float dissipationRate, float maxBuildup)
    {
        if (_net.IsClient)
            return;

        if (TryComp<RMCHivemindInterferenceImmuneComponent>(uid, out var immune))
        {
            immune.Remaining = Math.Min(immune.Remaining + immune.ReapplyExtension, immune.Max);
            return;
        }

        var isNew = !HasComp<RMCHivemindInterferenceComponent>(uid);
        var comp = EnsureComp<RMCHivemindInterferenceComponent>(uid);
        if (isNew)
        {
            comp.DissipationRate = dissipationRate;
            comp.MaxBuildup = maxBuildup;
        }
        else
        {
            comp.MaxBuildup = Math.Max(comp.MaxBuildup, maxBuildup);
        }

        comp.Magnitude = Math.Min(comp.Magnitude + magnitude, comp.MaxBuildup);
        Dirty(uid, comp);

        if (isNew)
            _popup.PopupEntity(Loc.GetString("rmc-hivemind-interference-start"), uid, uid, PopupType.LargeCaution);
    }

    public float GetRemainingSeconds(EntityUid uid)
    {
        if (!TryComp<RMCHivemindInterferenceComponent>(uid, out var comp) || comp.DissipationRate <= 0f)
            return 0f;

        return comp.Magnitude / comp.DissipationRate;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        if (time < _nextUpdate)
            return;

        _nextUpdate = time + UpdateInterval;
        var delta = (float) UpdateInterval.TotalSeconds;

        var query = EntityQueryEnumerator<RMCHivemindInterferenceComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Magnitude = MathF.Max(comp.Magnitude - comp.DissipationRate * delta, 0f);

            if (comp.Magnitude <= 0f)
            {
                RemCompDeferred<RMCHivemindInterferenceComponent>(uid);
                continue;
            }

            Dirty(uid, comp);
        }

        var immuneQuery = EntityQueryEnumerator<RMCHivemindInterferenceImmuneComponent>();
        while (immuneQuery.MoveNext(out var uid, out var immune))
        {
            immune.Remaining -= delta;
            if (immune.Remaining <= 0f)
                RemCompDeferred<RMCHivemindInterferenceImmuneComponent>(uid);
        }
    }
}

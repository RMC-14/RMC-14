using System.Numerics;
using Content.Shared._RMC14.Standing;
using Content.Shared.Buckle.Components;
using Content.Shared.Camera;
using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Content.Shared.Rejuvenate;
using Robust.Shared.Network;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Dizziness;

public sealed class RMCDizzinessSystem : EntitySystem
{
    private const float PixelsPerTile = 32f;

    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCDizzinessComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<RMCDizzinessComponent, RejuvenateEvent>(OnRejuvenate);
        SubscribeLocalEvent<RMCDizzinessComponent, GetEyeOffsetEvent>(OnGetEyeOffset);
    }

    private void OnMobStateChanged(Entity<RMCDizzinessComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState == MobState.Dead)
            ClearDizziness(ent);
    }

    private void OnRejuvenate(Entity<RMCDizzinessComponent> ent, ref RejuvenateEvent args)
    {
        ClearDizziness(ent);
    }

    private void OnGetEyeOffset(Entity<RMCDizzinessComponent> ent, ref GetEyeOffsetEvent args)
    {
        if (_net.IsServer || !ent.Comp.Dizzy || IsSteadied(ent))
            return;

        var dizziness = (double) ent.Comp.Dizziness;
        var worldTime = _timing.CurTime.TotalSeconds * 10;
        var amplitude = dizziness * (Math.Sin(DegreesToRadians(dizziness * 0.044 * worldTime)) + 1) / 70;
        var angle = DegreesToRadians(0.008 * dizziness * worldTime);
        var offset = new Vector2((float) (amplitude * Math.Sin(angle)), (float) (amplitude * Math.Cos(angle)));
        args.Offset += offset / PixelsPerTile;
    }

    public void MakeDizzy(EntityUid uid, float amount)
    {
        if (_net.IsClient || !HasComp<HumanoidAppearanceComponent>(uid))
            return;

        var comp = EnsureDizziness(uid);
        comp.Dizziness = MathF.Min(comp.Max, comp.Dizziness + amount);
        if (comp.Dizziness > comp.VisibleThreshold && !comp.Dizzy)
        {
            comp.Dizzy = true;
            comp.NextUnbearable = _timing.CurTime + comp.UnbearableEvery;
        }

        Dirty(uid, comp);
    }

    public float GetDizziness(EntityUid uid)
    {
        return TryComp<RMCDizzinessComponent>(uid, out var comp) ? comp.Dizziness : 0f;
    }

    public bool IsTooDizzyToFocus(EntityUid uid)
    {
        return TryComp<RMCDizzinessComponent>(uid, out var comp) && comp.Dizziness > comp.VisibleThreshold;
    }

    public void AddDizzinessCapped(EntityUid uid, float amount, float cap)
    {
        SetDizziness(uid, MathF.Min(GetDizziness(uid) + amount, cap));
    }

    public void ReduceDizziness(EntityUid uid, float amount)
    {
        if (!HasComp<RMCDizzinessComponent>(uid))
            return;

        SetDizziness(uid, GetDizziness(uid) - amount);
    }

    public void ClearDizziness(EntityUid uid)
    {
        if (!HasComp<RMCDizzinessComponent>(uid))
            return;

        SetDizziness(uid, 0f);
    }

    private void SetDizziness(EntityUid uid, float value)
    {
        if (_net.IsClient || !HasComp<HumanoidAppearanceComponent>(uid))
            return;

        value = MathF.Max(value, 0f);
        if (value <= 0f && !HasComp<RMCDizzinessComponent>(uid))
            return;

        var comp = EnsureDizziness(uid);
        comp.Dizziness = value;
        Dirty(uid, comp);
        UpdateDizzy((uid, comp));
    }

    private RMCDizzinessComponent EnsureDizziness(EntityUid uid)
    {
        if (TryComp<RMCDizzinessComponent>(uid, out var comp))
            return comp;

        comp = AddComp<RMCDizzinessComponent>(uid);
        comp.NextDecay = _timing.CurTime + comp.DecayEvery;
        return comp;
    }

    private void UpdateDizzy(Entity<RMCDizzinessComponent> ent)
    {
        if (ent.Comp.Dizzy && ent.Comp.Dizziness <= ent.Comp.VisibleThreshold)
        {
            ent.Comp.Dizzy = false;
            Dirty(ent);
            _popup.PopupEntity(Loc.GetString("rmc-dizziness-passed"), ent, ent);
        }

        if (ent.Comp.Dizziness <= 0f)
            RemCompDeferred<RMCDizzinessComponent>(ent);
    }

    private bool IsSteadied(EntityUid uid)
    {
        return TryComp(uid, out BuckleComponent? buckle) && buckle.Buckled ||
               TryComp(uid, out RMCRestComponent? rest) && rest.Resting;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees % 360 * Math.PI / 180;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var query = EntityQueryEnumerator<RMCDizzinessComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (comp.Dizzy && time >= comp.NextUnbearable)
            {
                comp.NextUnbearable = time + comp.UnbearableEvery;
                if (!IsSteadied(uid) && _random.Prob(comp.UnbearableChance))
                    _popup.PopupEntity(Loc.GetString("rmc-dizziness-unbearable"), uid, uid, PopupType.SmallCaution);
            }

            if (time < comp.NextDecay)
                continue;

            comp.NextDecay = time + comp.DecayEvery;

            var resting = TryComp(uid, out RMCRestComponent? rest) && rest.Resting;
            comp.Dizziness = MathF.Max(comp.Dizziness - (resting ? comp.RestingDecay : comp.Decay), 0f);
            Dirty(uid, comp);
            UpdateDizzy((uid, comp));
        }
    }
}

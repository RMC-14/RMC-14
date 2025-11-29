using Content.Shared.Damage;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Weapons.Ranged.Overheat;

public sealed class OverheatSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<OverheatComponent, GunShotEvent>(OnGunShot);
        SubscribeLocalEvent<OverheatComponent, AttemptShootEvent>(OnAttemptShoot);
        SubscribeLocalEvent<OverheatComponent, TryGainHeatEvent>(OnTryGainHeat);
        SubscribeLocalEvent<OverheatComponent, OverheatedEvent>(OnOverheated);
    }

    private void OnAttemptShoot(Entity<OverheatComponent> ent, ref AttemptShootEvent args)
    {
        if (ent.Comp.OverHeated)
            args.Cancelled = true;
    }

    private void OnGunShot(Entity<OverheatComponent> ent, ref GunShotEvent args)
    {
        var ev = new TryGainHeatEvent(ent.Comp.HeatPerShot);
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnTryGainHeat(Entity<OverheatComponent> ent, ref TryGainHeatEvent args)
    {
        ent.Comp.Heat = MathF.Max(0, ent.Comp.Heat + args.HeatGained);
        Dirty(ent);

        var heatGainedEvent = new HeatGainedEvent(ent.Comp.Heat);
        RaiseLocalEvent(ent, ref heatGainedEvent);

        if (ent.Comp.Heat < ent.Comp.MaxHeat)
            return;

        var overheatEvent = new OverheatedEvent(true, ent.Comp.Damage);
        RaiseLocalEvent(ent, ref overheatEvent);
    }

    private void OnOverheated(Entity<OverheatComponent> ent, ref OverheatedEvent args)
    {
        if (!args.OverHeated)
        {
            ent.Comp.OverHeated = false;
            var heatLost =  ent.Comp.Heat * ent.Comp.EmergencyCooldownMultiplier - ent.Comp.Heat;
            var ev = new TryGainHeatEvent(heatLost);
            RaiseLocalEvent(ent, ref ev);
        }
        else
        {
            ent.Comp.OverHeated = true;
            ent.Comp.OverHeatedAt = _time.CurTime;
            if (_net.IsServer)
                _audio.PlayPvs(ent.Comp.OverheatSound, ent);
        }

        Dirty(ent);
    }

    public override void Update(float frameTime)
    {
        var query = EntityQueryEnumerator<OverheatComponent>();

        while (query.MoveNext(out var uid, out var component))
        {
            if (component.Heat == 0)
                continue;

            if (!component.OverHeated)
            {
                var ev = new TryGainHeatEvent(-(component.CooldownRate * frameTime));
                RaiseLocalEvent(uid, ref ev);
            }
            else
            {
                if (_time.CurTime > component.OverHeatedAt + component.EmergencyCooldownDelay)
                {
                    var ev = new OverheatedEvent(false);
                    RaiseLocalEvent(uid, ref ev);
                }
            }
        }
    }
}

[ByRefEvent]
public record struct TryGainHeatEvent(float HeatGained);

[ByRefEvent]
public record struct HeatGainedEvent(float CurrentHeat);

/// <summary>
///     Raised when a weapon with the <see cref="OverheatComponent"/> changes its overheated state.
/// </summary>
/// <param name="OverHeated">True if the weapon is overheated, false if it has cooled down instead.</param>
/// <param name="Damage">The amount of damage to deal to the weapon</param>
[ByRefEvent]
public record struct OverheatedEvent(bool OverHeated, DamageSpecifier? Damage = null);

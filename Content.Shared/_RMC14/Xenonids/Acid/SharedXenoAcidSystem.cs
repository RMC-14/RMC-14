using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared._RMC14.Xenonids.Acid;

public abstract class SharedXenoAcidSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly XenoEnergySystem _xenoEnergy = default!;

    protected int CorrosiveAcidTickDelaySeconds;
    protected ProtoId<DamageTypePrototype> CorrosiveAcidDamageTypeStr = "Heat";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoAcidComponent, XenoCorrosiveAcidEvent>(OnXenoCorrosiveAcid);
        SubscribeLocalEvent<XenoAcidComponent, DoAfterAttemptEvent<XenoCorrosiveAcidDoAfterEvent>>(OnXenoCorrosiveAcidDoAfterAttempt);
        SubscribeLocalEvent<XenoAcidComponent, XenoCorrosiveAcidDoAfterEvent>(OnXenoCorrosiveAcidDoAfter);

        SubscribeLocalEvent<InheritAcidComponent, AmmoShotEvent>(OnAmmoShot);
        SubscribeLocalEvent<InheritAcidComponent, GrenadeContentThrownEvent>(OnGrenadeContentThrown);

        Subs.CVar(_config,
            RMCCVars.RMCCorrosiveAcidTickDelaySeconds,
            obj =>
            {
                CorrosiveAcidTickDelaySeconds = obj;
                OnXenoAcidSystemCVarsUpdated();
            },
            true);
        Subs.CVar(_config,
            RMCCVars.RMCCorrosiveAcidDamageType,
            obj =>
            {
                CorrosiveAcidDamageTypeStr = obj;
                OnXenoAcidSystemCVarsUpdated();
            },
            true);
    }

    private void OnXenoAcidSystemCVarsUpdated()
    {
        // If any of the relevant vars changed - we need to recalculate and update damage specifiers for all the corroding comps.
        // There is still a bit of a problem here - if AcidTickDelaySeconds changes, it will affect next tick damage-wise immediately while the time of the next tick will not change. It's an edge case though, I'd not expect anybody changing that CVar repeatedly during the round often enough for it to matter. So I'm not going to bother with it.
        var damageableCorrodingQuery = EntityQueryEnumerator<DamageableCorrodingComponent>();
        while (damageableCorrodingQuery.MoveNext(out var uid, out var damageableCorrodingComponent))
        {
            damageableCorrodingComponent.Damage = new(PrototypeManager.Index<DamageTypePrototype>(CorrosiveAcidDamageTypeStr), damageableCorrodingComponent.Dps * CorrosiveAcidTickDelaySeconds);
        }
    }

    private void OnXenoCorrosiveAcid(Entity<XenoAcidComponent> xeno, ref XenoCorrosiveAcidEvent args)
    {
        if (xeno.Owner != args.Performer ||
            !CheckCorrodiblePopups(xeno, args.Target, out var time, out var _))
        {
            return;
        }

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager, xeno, time * args.ApplyTimeMultiplier, new XenoCorrosiveAcidDoAfterEvent(args), xeno, args.Target)
        {
            BreakOnMove = true,
            RequireCanInteract = false,
            AttemptFrequency = AttemptFrequency.StartAndEnd
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnXenoCorrosiveAcidDoAfterAttempt(Entity<XenoAcidComponent> ent, ref DoAfterAttemptEvent<XenoCorrosiveAcidDoAfterEvent> args)
    {
        if (args.Cancelled)
            return;

        if (_mobState.IsIncapacitated(ent))
            args.Cancel();
    }

    private void OnXenoCorrosiveAcidDoAfter(Entity<XenoAcidComponent> xeno, ref XenoCorrosiveAcidDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (!CheckCorrodiblePopups(xeno, target, out var _, out var mult))
            return;

        if (args.PlasmaCost != 0 && !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
            return;

        if (args.EnergyCost != 0 && !_xenoEnergy.TryRemoveEnergyPopup(xeno.Owner, args.EnergyCost))
            return;

        if (_net.IsClient)
            return;

        args.Handled = true;

        ApplyAcid(args.AcidId, target, args.Dps, args.ExpendableLightDps, args.Time * mult);
    }

    /// <summary>
    ///     Transfer any acid stacks from the cartridge to the shot ammo.
    /// </summary>
    private void OnAmmoShot(Entity<InheritAcidComponent> ent, ref AmmoShotEvent args)
    {
        if(!TryComp(ent, out TimedCorrodingComponent? corroding))
            return;

        foreach (var projectile in args.FiredProjectiles)
        {
            ApplyAcid(corroding.AcidPrototype, projectile, corroding.LightDps, corroding.Dps, corroding.CorrodesAt, true);
        }
    }

    /// <summary>
    ///     Transfer any acid stacks from the grenade to the thrown contents.
    /// </summary>
    private void OnGrenadeContentThrown(Entity<InheritAcidComponent> ent, ref GrenadeContentThrownEvent args)
    {
        if (TryComp(args.Source, out TimedCorrodingComponent? corroding))
        {
            ApplyAcid(corroding.AcidPrototype,ent, corroding.Dps, corroding.LightDps, corroding.CorrodesAt, true);
        }
    }

    private bool CheckCorrodiblePopups(Entity<XenoAcidComponent> xeno, EntityUid target, out TimeSpan time, out float mult)
    {
        time = TimeSpan.Zero;
        mult = 1;
        if (!TryComp(target, out CorrodibleComponent? corrodible) ||
            !corrodible.IsCorrodible)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-acid-not-corrodible", ("target", target)), xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        if (HasComp<TimedCorrodingComponent>(target) || HasComp<DamageableCorrodingComponent>(target))
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-acid-already-corroding", ("target", target)), xeno, xeno);
            return false;
        }

        if (!xeno.Comp.CanMeltStructures && corrodible.Structure)
        {
            _popup.PopupClient(Loc.GetString("rmc-xeno-acid-structure-unmeltable"), xeno, xeno);
            return false;
        }

        time = corrodible.TimeToApply;
        mult = corrodible.MeltTimeMult;

        return true;
    }

    public void ApplyAcid(EntProtoId acidId, EntityUid target, float dps, float lightDps, TimeSpan time, bool inherit = false)
    {
        if (_net.IsClient)
            return;

        var acid = SpawnAttachedTo(acidId, target.ToCoordinates());

        if (!inherit)
            time += _timing.CurTime;

        var ev = new CorrodingEvent(acid, dps, lightDps);
        RaiseLocalEvent(target, ref ev);
        if (ev.Cancelled)
            return;

        AddComp(target, new TimedCorrodingComponent
        {
            Acid = acid,
            AcidPrototype = acidId,
            CorrodesAt = time,
            Dps = dps,
            LightDps = lightDps,
        });
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var damageableCorrodingQuery = EntityQueryEnumerator<DamageableCorrodingComponent>();
        while (damageableCorrodingQuery.MoveNext(out var uid, out var damageableCorrodingComponent))
        {
            if (time > damageableCorrodingComponent.NextDamageAt)
            {
                _damageable.TryChangeDamage(uid, damageableCorrodingComponent.Damage);
                damageableCorrodingComponent.NextDamageAt = time.Add(TimeSpan.FromSeconds(CorrosiveAcidTickDelaySeconds));
            }

            if (time > damageableCorrodingComponent.AcidExpiresAt)
            {
                QueueDel(damageableCorrodingComponent.Acid);
                RemCompDeferred<DamageableCorrodingComponent>(uid);
            }
        }

        var timedCorrodingQuery = EntityQueryEnumerator<TimedCorrodingComponent>();
        while (timedCorrodingQuery.MoveNext(out var uid, out var timedCorrodingComponent))
        {
            if (time < timedCorrodingComponent.CorrodesAt)
                continue;

            QueueDel(uid);
            QueueDel(timedCorrodingComponent.Acid);
        }
    }
}

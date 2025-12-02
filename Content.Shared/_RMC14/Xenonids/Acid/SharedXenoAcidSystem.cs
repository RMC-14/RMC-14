using System.Linq;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Chemistry;
using Content.Shared._RMC14.Xenonids.Energy;
using Content.Shared.Explosion.EntitySystems;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Xenonids.Acid;

public abstract class SharedXenoAcidSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedDropshipSystem _dropship = default!;
    [Dependency] private readonly SharedEntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
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
        var target = args.Target;
        string containerId = target switch
        {
            var e when TryComp<DropshipWeaponPointComponent>(e, out var weapon) => weapon.WeaponContainerSlotId,
            var e when TryComp<DropshipUtilityPointComponent>(e, out var utility) => utility.UtilitySlotId,
            var e when TryComp<DropshipElectronicSystemPointComponent>(e, out var electronic) => electronic.ContainerId,
            var e when TryComp<DropshipEnginePointComponent>(e, out var engine) => engine.ContainerId,
            _ => string.Empty
        };

        if (!string.IsNullOrEmpty(containerId))
        {
            if (_dropship.TryGetAttachmentContained(target, containerId, out var containedEntity))
                target = containedEntity;
            else
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-acid-not-corrodible", ("target", target)), xeno, xeno, PopupType.SmallCaution);
                return;
            }
        }

        if (xeno.Owner != args.Performer ||
            !CheckCorrodiblePopupsWithReplacement(xeno, target, args.Strength, out var time, out var _))
        {
            return;
        }

        args.Handled = true;

        var doAfter = new DoAfterArgs(EntityManager, xeno, time * args.ApplyTimeMultiplier, new XenoCorrosiveAcidDoAfterEvent(args), xeno, target)
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

        if (!TryComp(target, out CorrodibleComponent? corrodible) || !corrodible.IsCorrodible)
            return;

        if (!xeno.Comp.CanMeltStructures && corrodible.Structure)
            return;

        // Re-check if acid can be replaced at DoAfter end to prevent race conditions
        // (e.g., weak acid downgrading strong acid if both DoAfters were started before any completed)
        if (IsMelted(target) && !CanReplaceAcid(target, args.Strength))
            return;

        var mult = corrodible.MeltTimeMult;

        if (args.PlasmaCost != 0 && !_xenoPlasma.TryRemovePlasmaPopup(xeno.Owner, args.PlasmaCost))
            return;

        if (args.EnergyCost != 0 && !_xenoEnergy.TryRemoveEnergyPopup(xeno.Owner, args.EnergyCost))
            return;

        if (_net.IsClient)
            return;

        args.Handled = true;

        // Remove existing acid if present (we validated we can replace it above)
        if (_net.IsServer && IsMelted(target))
            RemoveAcid(target);

        ApplyAcid(args.AcidId, args.Strength, target, args.Dps, args.ExpendableLightDps, args.Time * mult);
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
            ApplyAcid(corroding.AcidPrototype, corroding.Strength, projectile, corroding.LightDps, corroding.Dps, corroding.CorrodesAt, true);
        }
    }

    /// <summary>
    ///     Transfer any acid stacks from the grenade to the thrown contents.
    /// </summary>
    private void OnGrenadeContentThrown(Entity<InheritAcidComponent> ent, ref GrenadeContentThrownEvent args)
    {
        if (TryComp(args.Source, out TimedCorrodingComponent? corroding))
        {
            ApplyAcid(corroding.AcidPrototype, corroding.Strength, ent, corroding.Dps, corroding.LightDps, corroding.CorrodesAt, true);
        }
    }

    private bool CheckCorrodiblePopupsWithReplacement(Entity<XenoAcidComponent> xeno, EntityUid target, XenoAcidStrength newStrength, out TimeSpan time, out float mult)
    {
        time = TimeSpan.Zero;
        mult = 1;
        if (!TryComp(target, out CorrodibleComponent? corrodible) ||
            !corrodible.IsCorrodible)
        {
            _popup.PopupClient(Loc.GetString("cm-xeno-acid-not-corrodible", ("target", target)), xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        // Check if acid already exists and if new acid can replace it
        if (IsMelted(target))
        {
            if (!CanReplaceAcid(target, newStrength))
            {
                _popup.PopupClient(Loc.GetString("cm-xeno-acid-already-corroding", ("target", target)), xeno, xeno);
                return false;
            }
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

    public void ApplyAcid(EntProtoId acidId, XenoAcidStrength strength, EntityUid target, float dps, float lightDps, TimeSpan time, bool inherit = false)
    {
        if (_net.IsClient)
            return;

        EntityUid acid;
        if (HasComp<VisiblyAcidOutsideContainerComponent>(target) &&
            _container.TryGetContainingContainer(target, out var container))
        {
            acid = SpawnAttachedTo(acidId, container.Owner.ToCoordinates());
        }
        else
        {
            acid = SpawnAttachedTo(acidId, target.ToCoordinates());
        }

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
            Strength = strength,
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
                var ev = new BeforeMeltedEvent();
                RaiseLocalEvent(uid, ref ev);

                QueueDel(damageableCorrodingComponent.Acid);
                RemCompDeferred<DamageableCorrodingComponent>(uid);
            }
        }

        var timedCorrodingQuery = EntityQueryEnumerator<TimedCorrodingComponent>();
        while (timedCorrodingQuery.MoveNext(out var uid, out var timedCorrodingComponent))
        {
            if (time < timedCorrodingComponent.CorrodesAt)
                continue;

            var ev = new BeforeMeltedEvent();
            RaiseLocalEvent(uid, ref ev);

            _entityStorage.EmptyContents(uid);

            if (TryComp(uid, out StorageComponent? storage))
            {
                foreach (var contained in storage.Container.ContainedEntities.ToArray())
                {
                    if (!TryComp(contained, out CorrodibleComponent? corrodible) ||
                        !corrodible.IsCorrodible)
                    {
                        _container.Remove(contained, storage.Container);
                    }
                }
            }

            QueueDel(uid);
            QueueDel(timedCorrodingComponent.Acid);
        }
    }

    public bool IsMelted(EntityUid uid)
    {
        return HasComp<TimedCorrodingComponent>(uid) || HasComp<DamageableCorrodingComponent>(uid);
    }

    public bool CanReplaceAcid(EntityUid target, XenoAcidStrength newStrength)
    {
        // Get existing acid strength from the component
        XenoAcidStrength? existingStrength = null;
        
        if (TryComp<TimedCorrodingComponent>(target, out var timedCorroding))
            existingStrength = timedCorroding.Strength;
        else if (TryComp<DamageableCorrodingComponent>(target, out var damageableCorroding))
            existingStrength = damageableCorroding.Strength;

        if (existingStrength == null)
            return true;

        // Simple comparison - can only replace if new acid is stronger
        return newStrength > existingStrength;
    }

    public void RemoveAcid(EntityUid uid)
    {
        if (TryComp<TimedCorrodingComponent>(uid, out var timed))
        {
            QueueDel(timed.Acid);
            RemComp<TimedCorrodingComponent>(uid);
        }

        if (TryComp<DamageableCorrodingComponent>(uid, out var damageable))
        {
            QueueDel(damageable.Acid);
            RemComp<DamageableCorrodingComponent>(uid);
        }
    }


}

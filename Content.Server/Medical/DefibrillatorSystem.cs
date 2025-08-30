using Content.Server.Atmos.Rotting;
using Content.Server.Chat.Systems;
using Content.Server.DoAfter;
using Content.Server.Electrocution;
using Content.Server.EUI;
using Content.Server.Ghost;
using Content.Server.Popups;
using Content.Server.PowerCell;
using Content.Shared._RMC14.Damage;
using Content.Shared._RMC14.Medical.Defibrillator;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Inventory;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Medical;
using Content.Shared.Mind;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Timing;
using Content.Shared.Traits.Assorted;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Player;

namespace Content.Server.Medical;

/// <summary>
/// This handles interactions and logic relating to <see cref="DefibrillatorComponent"/>
/// </summary>
public sealed class DefibrillatorSystem : EntitySystem
{
    [Dependency] private readonly ChatSystem _chatManager = default!;
    [Dependency] private readonly SharedRMCDamageableSystem _rmcDamageable = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly ElectrocutionSystem _electrocution = default!;
    [Dependency] private readonly EuiManager _euiManager = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly ItemToggleSystem _toggle = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThreshold = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly PowerCellSystem _powerCell = default!;
    [Dependency] private readonly RottingSystem _rotting = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly CMDefibrillatorSystem _cmDefibrillator = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<DefibrillatorComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<DefibrillatorComponent, DefibrillatorZapDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(EntityUid uid, DefibrillatorComponent component, AfterInteractEvent args)
    {
        if (args.Handled || args.Target is not { } target)
            return;

        args.Handled = TryStartZap(uid, target, args.User, component);
    }

    private void OnDoAfter(EntityUid uid, DefibrillatorComponent component, DefibrillatorZapDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            _cmDefibrillator.StopChargingAudio((uid, component));
            return;
        }

        if (args.Target is not { } target)
            return;

        if (!CanZap(uid, target, args.User, component))
            return;

        args.Handled = true;
        Zap(uid, target, args.User, component);
    }

    /// <summary>
    ///     Checks if you can actually defib a target.
    /// </summary>
    /// <param name="uid">Uid of the defib</param>
    /// <param name="target">Uid of the target getting defibbed</param>
    /// <param name="user">Uid of the entity using the defibrillator</param>
    /// <param name="component">Defib component</param>
    /// <param name="targetCanBeAlive">
    ///     If true, the target can be alive. If false, the function will check if the target is alive and will return false if they are.
    /// </param>
    /// <returns>
    ///     Returns true if the target is valid to be defibed, false otherwise.
    /// </returns>
    public bool CanZap(EntityUid uid, EntityUid target, EntityUid? user = null, DefibrillatorComponent? component = null, bool targetCanBeAlive = false)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!_toggle.IsActivated(uid))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("defibrillator-not-on"), uid, user.Value);
            return false;
        }

        if (!TryComp(uid, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((uid, useDelay), component.DelayId))
            return false;

        if (!TryComp<MobStateComponent>(target, out var mobState))
            return false;

        if (!_powerCell.HasActivatableCharge(uid, user: user))
            return false;

        if (!targetCanBeAlive && _mobState.IsAlive(target, mobState))
            return false;

        if (!targetCanBeAlive && !component.CanDefibCrit && _mobState.IsCritical(target, mobState))
            return false;

        if (TryComp(target, out CMDefibrillatorBlockedComponent? block))
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString(block.Popup, ("target", target)), uid, user.Value);
            return false;
        }

        var slots = _inventory.GetSlotEnumerator(target, SlotFlags.OUTERCLOTHING);
        while (slots.MoveNext(out var slot))
        {
            if (TryComp(slot.ContainedEntity, out CMDefibrillatorBlockedComponent? comp))
            {
                if (user != null)
                    _popup.PopupEntity(Loc.GetString(comp.Popup, ("target", target)), uid, user.Value);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    ///     Tries to start defibrillating the target. If the target is valid, will start the defib do-after.
    /// </summary>
    /// <param name="uid">Uid of the defib</param>
    /// <param name="target">Uid of the target getting defibbed</param>
    /// <param name="user">Uid of the entity using the defibrillator</param>
    /// <param name="component">Defib component</param>
    /// <returns>
    ///     Returns true if the defibrillation do-after started, otherwise false.
    /// </returns>
    public bool TryStartZap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        if (!CanZap(uid, target, user, component))
            return false;

        _cmDefibrillator.StopChargingAudio((uid, component));
        component.ChargeSoundEntity = _audio.PlayPvs(component.ChargeSound, uid)?.Entity;
        if (component.ChargeSoundEntity is { } sound)
        {
            var audio = EnsureComp<RMCDefibrillatorAudioComponent>(sound);
#pragma warning disable RA0002
            audio.Defibrillator = uid;
#pragma warning restore RA0002
            Dirty(sound, audio);
        }

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager, user, component.DoAfterDuration, new DefibrillatorZapDoAfterEvent(),
            uid, target, uid)
        {
            NeedHand = true,
            BreakOnMove = !component.AllowDoAfterMovement,
            // RMC14
            BreakOnHandChange = false,
            DuplicateCondition = DuplicateConditions.SameEvent,
            TargetEffect = "RMCEffectHealBusy",
            MovementThreshold = 0.5f,
        });
    }

    /// <summary>
    ///     Tries to defibrillate the target with the given defibrillator.
    /// </summary>
    public void Zap(EntityUid uid, EntityUid target, EntityUid user, DefibrillatorComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var selfEvent = new SelfBeforeDefibrillatorZapsEvent(user, uid, target);
        RaiseLocalEvent(user, selfEvent);

        target = selfEvent.DefibTarget;

        // Ensure thet new target is still valid.
        if (selfEvent.Cancelled || !CanZap(uid, target, user, component, true))
            return;

        var targetEvent = new TargetBeforeDefibrillatorZapsEvent(user, uid, target);
        RaiseLocalEvent(target, targetEvent);

        target = targetEvent.DefibTarget;

        if (targetEvent.Cancelled || !CanZap(uid, target, user, component, true))
            return;

        if (!TryComp<MobStateComponent>(target, out var mob) ||
            !TryComp<MobThresholdsComponent>(target, out var thresholds))
            return;

        // RMC14 to fix last charge not being usable
        if (!_powerCell.TryUseActivatableCharge(uid, user: user))
            return;

        _audio.PlayPvs(component.ZapSound, uid);
        _electrocution.TryDoElectrocution(target, null, component.ZapDamage, component.WritheDuration, true, ignoreInsulation: true);
        if (!TryComp<UseDelayComponent>(uid, out var useDelay))
            return;
        _useDelay.SetLength((uid, useDelay), component.ZapDelay, component.DelayId);
        _useDelay.TryResetDelay((uid, useDelay), id: component.DelayId);

        ICommonSession? session = null;

        var dead = true;
        if (_rotting.IsRotten(target))
        {
            _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-rotten"),
                InGameICChatType.Speak, true);
        }
        else if (TryComp<UnrevivableComponent>(target, out var unrevivable))
        {
            _chatManager.TrySendInGameICMessage(uid, Loc.GetString(unrevivable.ReasonMessage),
                InGameICChatType.Speak, true);
        }
        else
        {
            if (_mobState.IsDead(target, mob))
            {
                var heal = new DamageSpecifier(component.ZapHeal);
                if (component.CMZapDamage != null)
                {
                    foreach (var (group, amount) in component.CMZapDamage)
                    {
                        heal = _rmcDamageable.DistributeDamage(target, group, amount, heal);
                    }
                }

                _damageable.TryChangeDamage(target, heal, true, origin: uid);
            }

            if (_mobThreshold.TryGetThresholdForState(target, MobState.Dead, out var threshold) &&
                TryComp<DamageableComponent>(target, out var damageableComponent) &&
                damageableComponent.TotalDamage < threshold)
            {
                _mobState.ChangeMobState(target, MobState.Critical, mob, uid);
                dead = false;
            }

            if (_mind.TryGetMind(target, out _, out var mind) &&
                _player.TryGetSessionById(mind.UserId, out var playerSession))
            {
                session = playerSession;
                // notify them they're being revived.
                if (mind.CurrentEntity != target)
                {
                    _euiManager.OpenEui(new ReturnToBodyEui(mind, _mind, _player), session);
                }
            }
            else
            {
                _chatManager.TrySendInGameICMessage(uid, Loc.GetString("defibrillator-no-mind"),
                    InGameICChatType.Speak, true);
            }
        }

        var sound = dead || session == null
            ? component.FailureSound
            : component.SuccessSound;
        _audio.PlayPvs(sound, uid);

        // if we don't have enough power left for another shot, turn it off
        if (!_powerCell.HasActivatableCharge(uid))
            _toggle.TryDeactivate(uid);

        // TODO clean up this clown show above
        var ev = new TargetDefibrillatedEvent(user, (uid, component));
        RaiseLocalEvent(target, ref ev);
    }
}

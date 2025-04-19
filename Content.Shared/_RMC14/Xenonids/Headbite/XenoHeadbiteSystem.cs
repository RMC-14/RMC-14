using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Xenonids.Heal;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Jittering;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Headbite;

public sealed class XenoHeadbiteSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly MobThresholdSystem _mobThresholds = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedXenoHealSystem _xenoHeal = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly SharedJitteringSystem _jitter = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;

    private static readonly ProtoId<DamageTypePrototype> LethalDamageType = "Asphyxiation";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoHeadbiteComponent, XenoHeadbiteActionEvent>(OnXenoHeadbiteAction);
        SubscribeLocalEvent<XenoHeadbiteComponent, XenoHeadbiteDoAfterEvent>(OnXenoHeadbiteDoAfter);
    }

    private void OnXenoHeadbiteAction(Entity<XenoHeadbiteComponent> xeno, ref XenoHeadbiteActionEvent args)
    {
        var target = args.Target;

        if (!CanHeadbite(xeno, target))
            return;

        var doAfter = new DoAfterArgs(EntityManager, xeno, xeno.Comp.HeadbiteDelay, new XenoHeadbiteDoAfterEvent(), xeno, target)
        {
            BreakOnMove = true,
            BreakOnDamage = false,
            ForceVisible = true,
            CancelDuplicate = true,
            DuplicateCondition = DuplicateConditions.SameEvent
        };

        var selfMsg = Loc.GetString("rmc-xeno-headbite-self", ("xeno", xeno.Owner), ("target", target));
        _popup.PopupClient(selfMsg, xeno, xeno, PopupType.Medium);

        var othersMsg = Loc.GetString("rmc-xeno-headbite-others", ("xeno", xeno.Owner), ("target", target));
        _popup.PopupEntity(othersMsg, xeno, Filter.PvsExcept(xeno), true, PopupType.MediumCaution);

        if (_doAfter.TryStartDoAfter(doAfter))
            args.Handled = true;
    }

    private void OnXenoHeadbiteDoAfter(Entity<XenoHeadbiteComponent> xeno, ref XenoHeadbiteDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        if (!CanHeadbite(xeno, target))
            return;

        args.Handled = true;

        if (_net.IsServer)
        {
            SpawnAttachedTo(xeno.Comp.HealEffect, xeno.Owner.ToCoordinates());
            SpawnAttachedTo(xeno.Comp.HeadbiteEffect, target.ToCoordinates());
            _emote.TryEmoteWithChat(xeno, xeno.Comp.Emote, cooldown: xeno.Comp.EmoteCooldown);
            _audio.PlayPvs(xeno.Comp.HitSound, xeno);
        }

        _xenoHeal.CreateHealStacks(xeno, xeno.Comp.HealAmount, xeno.Comp.HealDelay, 1, xeno.Comp.HealDelay);
        _jitter.DoJitter(xeno, xeno.Comp.JitterTime, true, 80, 8, true);

        var change = _damage.TryChangeDamage(target, xeno.Comp.Damage); // TODO target head
        if (change?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(target, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno.Owner);
            _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
        }

        if (_mobThresholds.TryGetDeadThreshold(target, out var mobThreshold) && TryComp<DamageableComponent>(target, out var damageable))
        {
            var lethalAmountOfDamage = mobThreshold.Value - damageable.TotalDamage;
            var type = _prototypeManager.Index<DamageTypePrototype>(LethalDamageType);
            var damage = new DamageSpecifier(type, lethalAmountOfDamage);
            _damage.TryChangeDamage(target, damage, true, origin: xeno);
        }

        var selfMsg = Loc.GetString("rmc-xeno-headbite-hit-self", ("xeno", xeno.Owner), ("target", target));
        _popup.PopupClient(selfMsg, xeno, xeno, PopupType.Medium);

        var othersMsg = Loc.GetString("rmc-xeno-headbite-hit-others", ("xeno", xeno.Owner), ("target", target));
        _popup.PopupEntity(othersMsg, xeno, Filter.PvsExcept(xeno), true, PopupType.MediumCaution);
    }

    private bool CanHeadbite(EntityUid xeno, EntityUid target)
    {
        if (!_mobState.IsCritical(target))
        {
            var failMsg = Loc.GetString("rmc-xeno-headbite-warning");
            _popup.PopupClient(failMsg, xeno, xeno, PopupType.SmallCaution);
            return false;
        }

        if (HasComp<XenoComponent>(xeno))
        {
            if (TryComp<VictimInfectedComponent>(target, out var victim) && _hive.IsMember(xeno, victim.Hive))
            {
                var failMsg = Loc.GetString("rmc-xeno-headbite-warning-larva");
                _popup.PopupClient(failMsg, xeno, xeno, PopupType.SmallCaution);
                return false;
            }
        }

        return true;
    }
}

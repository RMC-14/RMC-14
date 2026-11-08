using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Emote;
using Content.Shared._RMC14.Weapons.Melee;
using Content.Shared._RMC14.Xenonids.Finesse;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Xenonids.Impale;

public sealed class XenoImpaleSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCEmoteSystem _emote = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _flash = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedRMCMeleeWeaponSystem _rmcMelee = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly XenoSystem _xeno = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;


    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoImpaleComponent, XenoImpaleActionEvent>(OnXenoImpaleAction);
    }

    private void OnXenoImpaleAction(Entity<XenoImpaleComponent> xeno, ref XenoImpaleActionEvent args)
    {
        if (args.Handled)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        bool criticalMark = false;

        if (TryComp<XenoMarkedComponent>(args.Target, out var mark))
        {
            criticalMark = mark.IsCriticalTag;

            if (criticalMark)
                _popup.PopupEntity(Loc.GetString("rmc-xeno-marked-critical-consumed"), args.Target, args.Target, PopupType.SmallCaution);

            if (xeno.Comp.Emote is { } emote)
                _emote.TryEmoteWithChat(xeno, emote, cooldown: xeno.Comp.EmoteCooldown);

            var secondHit = EnsureComp<XenoSecondImpaleComponent>(args.Target);
            secondHit.ExtraImpales.Add((_timing.CurTime + xeno.Comp.SecondImpaleTime, xeno.Comp.Damage, xeno));

            RemCompDeferred<XenoMarkedComponent>(args.Target);
        }

        args.Handled = !criticalMark;

        Impale(xeno.Comp.Damage, xeno.Comp.AP, xeno.Comp.Animation, xeno.Comp.Sound, args.Target, xeno);

    }

    private void Impale(DamageSpecifier damage, int aP, EntProtoId animation, SoundSpecifier sound, EntityUid target, EntityUid xeno)
    {
        //TODO RMC14 targets chest
        var damageTaken = _damage.TryChangeDamage(target, _xeno.TryApplyXenoSlashDamageMultiplier(target, damage), armorPiercing: aP, origin: xeno, tool: xeno);
        if (damageTaken?.GetTotal() > FixedPoint2.Zero)
        {
            var filter = Filter.Pvs(target, entityManager: EntityManager).RemoveWhereAttachedEntity(o => o == xeno);
            _flash.RaiseEffect(Color.Red, new List<EntityUid> { target }, filter);
        }

        _rmcMelee.DoLunge(xeno, target);

        if (_net.IsClient)
            return;

        _audio.PlayPvs(sound, xeno);
        SpawnAttachedTo(animation, target.ToCoordinates());
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;

        var impaleQuery = EntityQueryEnumerator<XenoSecondImpaleComponent>();

        while (impaleQuery.MoveNext(out var uid, out var impale))
        {
            List<(TimeSpan, DamageSpecifier, EntityUid)> removeList = new();

            foreach (var newImpale in impale.ExtraImpales)
            {
                if (newImpale.ImpaleAt > time)
                    continue;

                Impale(newImpale.Damage, impale.AP, impale.Animation, impale.Sound, uid, newImpale.Origin);
                removeList.Add(newImpale);
            }

            foreach (var toRemove in removeList)
                impale.ExtraImpales.Remove(toRemove);

            if (impale.ExtraImpales.Count == 0)
                RemCompDeferred<XenoSecondImpaleComponent>(uid);
        }
    }
}

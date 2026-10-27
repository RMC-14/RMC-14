using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Random;
using Content.Shared._RMC14.Stun;
using Content.Shared.Body.Components;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.CombatMode;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Effects;
using Content.Shared.FixedPoint;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.NPC.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.Syringe;

public sealed class RMCInjectorSystem : EntitySystem
{
    [Dependency] private readonly NpcFactionSystem _npcFaction = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedCombatModeSystem _combatSystem = default!;
    [Dependency] private readonly MobStateSystem _mob = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedInjectorSystem _injector = default!;
    [Dependency] private readonly SharedSolutionContainerSystem _solution = default!;
    [Dependency] private readonly SharedHandsSystem _hands = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedColorFlashEffectSystem _colorFlash = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCSyringeComponent, MeleeHitEvent>(OnSyringeCombat);
        SubscribeLocalEvent<RMCSyringeComponent, RMCSyringeGetDelayEvent>(OnSyringeGetDelay);
    }

    private void OnSyringeCombat(Entity<RMCSyringeComponent> syringe, ref MeleeHitEvent args)
    {
        foreach (var hit in args.HitEntities)
        {
            args.Handled = true;

            if (!HasComp<MobStateComponent>(hit))
                continue;

            // CQC cancel
            if (hit != args.User && _combatSystem.IsInCombatMode(hit) &&
                _mob.IsAlive(hit) && !HasComp<StunnedComponent>(hit) &&
                !HasComp<RMCUnconsciousComponent>(hit) &&
                _skills.HasSkill(hit, syringe.Comp.CQCSkill, syringe.Comp.CQCMinFailLevel))
            {
                _stun.TryParalyze(args.User, syringe.Comp.CQCKnockdown, true);
                _audio.PlayPredicted(syringe.Comp.CQCSuccessSound, hit, args.User);

                _popup.PopupClient(Loc.GetString("rmc-syringe-combat-cqcd-self", ("target", Identity.Name(hit, EntityManager, args.User).Value), ("injector", syringe)), args.User, PopupType.SmallCaution);

                foreach (var session in Filter.PvsExcept(args.User, entityManager: EntityManager).Recipients)
                {
                    if (session.AttachedEntity is not { } viewer)
                        continue;

                    var message = "rmc-syringe-combat-cqcd";
                    if (viewer == hit)
                        message = "rmc-syringe-combat-cqcd-target";

                    var targetname = Identity.Name(hit, EntityManager, viewer);
                    var userName = Identity.Name(args.User, EntityManager, viewer);

                    var othersMsg = Loc.GetString(message, ("user", userName), ("target", targetname), ("injector", syringe));
                    _popup.PopupEntity(othersMsg, hit, session, PopupType.MediumCaution);
                }
                return;
            }

            // Humanoids only
            if (HasComp<MarineComponent>(hit) && hit != args.User)
            {
                // TODO RMC14 check targeted limb
                var ev = new CMGetArmorEvent(SlotFlags.OUTERCLOTHING | SlotFlags.INNERCLOTHING);
                RaiseLocalEvent(hit, ref ev);

                // TODO RMC14 replace with predictedrandom
                var seed = (long)1 << 32 | GetNetEntity(args.User).Id;
                var random = new Xoshiro128P(seed, (long)_timing.CurTick.Value << 32 | GetNetEntity(hit).Id).NextFloat(0f, 1f);

                if (ev.Melee > syringe.Comp.MinArmorBlock && random < syringe.Comp.ArmorFailChance)
                {
                    _audio.PlayPredicted(syringe.Comp.ArmorSound, hit, args.User);
                    _popup.PopupClient(Loc.GetString("rmc-syringe-combat-armor", ("target", Identity.Name(hit, EntityManager, args.User).Value), ("injector", syringe)), args.User, PopupType.SmallCaution);
                    BreakSyringe(syringe, args.User);
                    return;
                }
            }

            var damage = _damage.TryChangeDamage(hit, syringe.Comp.InjectDamage, true, origin: args.User, tool: syringe);

            if (damage?.GetTotal() > FixedPoint2.Zero)
            {
                var filter = Filter.Pvs(hit, entityManager: EntityManager);
                _colorFlash.RaiseEffect(Color.Red, new List<EntityUid> { hit }, filter);
            }

            if (HasComp<InjectableSolutionComponent>(hit) &&
                TryComp<InjectorComponent>(syringe, out var inject) &&
                _solution.TryGetSolution(syringe.Owner, inject.SolutionName, out _, out var solu))
            {
                inject.TransferAmount = _random.Next(0, Math.Max(0, solu.Volume.Int() - syringe.Comp.CombatInjectPenalty));
                _injector.TryForceInject((syringe, inject), hit, args.User);

                _popup.PopupClient(Loc.GetString("rmc-syringe-combat-success", ("target", Identity.Name(hit, EntityManager, args.User).Value), ("injector", syringe)), args.User, PopupType.SmallCaution);
                _popup.PopupEntity(Loc.GetString("rmc-syringe-combat-success-target", ("user", Identity.Name(args.User, EntityManager, hit).Value), ("injector", syringe)), hit, hit, PopupType.MediumCaution);
            }

            BreakSyringe(syringe, args.User);
        }
    }

    private void BreakSyringe(Entity<RMCSyringeComponent> syringe, EntityUid user)
    {
        if (_net.IsClient)
            return;

        var broken = SpawnAtPosition(syringe.Comp.BrokenSyringe, syringe.Owner.ToCoordinates());
        _audio.PlayEntity(syringe.Comp.BreakSound, user, broken);

        foreach (var hand in _hands.EnumerateHands(user))
        {
            if (_hands.GetHeldItem(user, hand) == syringe)
            {
                _hands.TryForcePickup(user, broken, hand, false, false);
                break;
            }
        }
        QueueDel(syringe);
    }

    private void OnSyringeGetDelay(Entity<RMCSyringeComponent> syringe, ref RMCSyringeGetDelayEvent args)
    {
        if (syringe.Comp.AllowInstantSelfInject &&
            args.Mode == InjectorToggleMode.Inject &&
            args.User == args.Target)
        {
            args.Delay = TimeSpan.Zero;
            return;
        }

        if (args.Mode == InjectorToggleMode.Draw)
        {
            // Check target - if in combat mode abort!!!
            // Not parity but just in case

            if (syringe.Comp.NoDrawOnAliveHostiles && _combatSystem.IsInCombatMode(args.Target) && !_mob.IsDead(args.Target))
            {
                args.Cancelled = true;
                _popup.PopupEntity(Loc.GetString("rmc-syringe-no-hostile-draw", ("target", Identity.Name(args.Target, EntityManager, args.User))), args.User, args.User, PopupType.SmallCaution);
                return;
            }

            if (!syringe.Comp.AllowBloodDraw && HasComp<BloodstreamComponent>(args.Target))
            {
                args.Cancelled = true;
                _popup.PopupEntity(Loc.GetString("rmc-syringe-no-blood-draw", ("injector", syringe)), args.User, args.User, PopupType.SmallCaution);
                return;
            }

            if (syringe.Comp.AllowInstantDraw)
            {
                args.Delay = TimeSpan.Zero;
                return;
            }
        }

        // If nothing else goes through, do skill check
        if (syringe.Comp.SkillBasedDelay)
            args.Delay *= _skills.GetSkillDelayMultiplier(args.User, syringe.Comp.SkillCheck);
    }
}

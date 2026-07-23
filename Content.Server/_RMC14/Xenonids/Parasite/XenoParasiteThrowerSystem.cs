using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Construction.EggMorpher;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Actions;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using System.Linq;
using System.Numerics;

namespace Content.Server._RMC14.Xenonids.Parasite;

public sealed class XenoParasiteThrowerSystem : SharedXenoParasiteThrowerSystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly SharedXenoParasiteSystem _parasite = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _rmcObstacleSlamming = default!;
    [Dependency] private readonly EggMorpherSystem _morpher = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoThrowParasiteActionEvent>(OnToggleParasiteThrow);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, MobStateChangedEvent>(OnMobStateChanged);

        SubscribeLocalEvent<XenoParasiteThrowerComponent, UserActivateInWorldEvent>(OnXenoParasiteThrowerUseInHand);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoDevolveBuiMsg>(OnXenoDevolveDoAfter);
    }

    private void OnToggleParasiteThrow(Entity<XenoParasiteThrowerComponent> xeno, ref XenoThrowParasiteActionEvent args)
    {
        var target = args.Target;

        args.Handled = true;

        _action.SetUseDelay((args.Action, args.Action), TimeSpan.Zero);

        // If none of the entities on the selected, in-range tile are parasites, try to pull out a
        // parasite OR try to throw a held parasite
        if (_interact.InRangeUnobstructed(xeno, target))
        {
            var clickedEntities = _lookup.GetEntitiesIntersecting(target);
            var tileHasParasites = false;
            var tileHasMorpher = false;

            foreach (var possibleParasiteOrMorpher in clickedEntities)
            {
                if (TryComp<EggMorpherComponent>(possibleParasiteOrMorpher, out var morpher) && _hive.FromSameHive(xeno.Owner, possibleParasiteOrMorpher))
                {
                    if (morpher.CurParasites <= 0 || xeno.Comp.CurParasites >= xeno.Comp.MaxParasites)
                        continue;

                    if (HasComp<OnFireComponent>(xeno))
                    {
                        _popup.PopupEntity(Loc.GetString("rmc-xeno-throw-parasite-empty-on-fire", ("morpher", possibleParasiteOrMorpher)), xeno, args.Performer, PopupType.MediumCaution);
                        continue;
                    }

                    _morpher.EggMorpherEmpty((possibleParasiteOrMorpher, morpher), xeno);

                    tileHasMorpher = true;
                    continue;
                }

                if (_mobState.IsDead(possibleParasiteOrMorpher))
                    continue;

                if (!HasComp<XenoParasiteComponent>(possibleParasiteOrMorpher))
                    continue;

                if (!HasComp<ParasiteAIComponent>(possibleParasiteOrMorpher))
                    continue;

                tileHasParasites = true;

                if (xeno.Comp.CurParasites >= xeno.Comp.MaxParasites)
                {
                    _popup.PopupEntity(Loc.GetString("cm-xeno-throw-parasite-too-many-parasites"), xeno, xeno);
                    return;
                }

                AddParasite(possibleParasiteOrMorpher, xeno);
            }

            if (tileHasParasites)
            {
                var stashMsg = Loc.GetString("cm-xeno-throw-parasite-stash-parasite", ("cur_parasites", xeno.Comp.CurParasites), ("max_parasites", xeno.Comp.MaxParasites));
                _popup.PopupEntity(stashMsg, xeno, xeno);
                return;
            }

            if (tileHasMorpher)
                return;
        }

        if (_hands.GetActiveItem((xeno, null)) is { } heldEntity &&
            HasComp<XenoParasiteComponent>(heldEntity))
        {
            _hands.TryDrop(xeno.Owner);
            var coords = _transform.GetMoverCoordinates(xeno);
            // If throw distance would be more than 4, fix it to be exactly 4
            if (coords.TryDistance(EntityManager, target, out var dis) && dis > xeno.Comp.ParasiteThrowDistance)
            {
                var fixedTrajectory = (target.Position - coords.Position).Normalized() * xeno.Comp.ParasiteThrowDistance;
                target = coords.WithPosition(coords.Position + fixedTrajectory);
            }

            _rmcObstacleSlamming.MakeImmune(heldEntity);
            _throw.TryThrow(heldEntity, target, user: xeno);

            // Not parity but should help the ability be more consistent/not look weird since para AI goes rest on idle.
            // Should amount to about 10 seconds before they attempt a leap (10 seconds stunned)
            // Average in parity is waiting 7.5 if you're lucky on idle time which would take 10 seconds still
            if (TryComp<ParasiteAIComponent>(heldEntity, out var ai) && !_mobState.IsDead(heldEntity))
            {
                _stun.TryStun(heldEntity, xeno.Comp.ThrownParasiteStunDuration * 2, true);
                _parasite.GoActive((heldEntity, ai));
            }

            _action.SetUseDelay((args.Action, args.Action), xeno.Comp.ThrownParasiteCooldown);

            return;
        }

        if (xeno.Comp.CurParasites == 0)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-throw-parasite-no-parasites"), xeno, xeno);
            return;
        }

        if (!_hands.TryGetEmptyHand(xeno.Owner, out _))
            return;

        if (HasComp<OnFireComponent>(xeno))
        {
            _popup.PopupEntity("Retrieving a stored parasite while we're on fire would burn it!", xeno, args.Performer, PopupType.MediumCaution);
            return;
        }

        if (RemoveParasite(xeno) is not { } newParasite)
            return;

        _hive.SetSameHive(xeno.Owner, newParasite);

        _hands.TryPickupAnyHand(xeno, newParasite);

        var msg = Loc.GetString("cm-xeno-throw-parasite-unstash-parasite", ("cur_parasites", xeno.Comp.CurParasites), ("max_parasites", xeno.Comp.MaxParasites));
        _popup.PopupEntity(msg, xeno, xeno);
    }

    private void OnXenoParasiteThrowerUseInHand(Entity<XenoParasiteThrowerComponent> xeno, ref UserActivateInWorldEvent args)
    {
        var target = args.Target;

        if (!HasComp<XenoParasiteComponent>(target))
            return;

        if (_mobState.IsDead(target))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-dead-child"), xeno, xeno);
            return;
        }

        if (args.Handled)
            return;

        if (xeno.Comp.CurParasites >= xeno.Comp.MaxParasites)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-throw-parasite-too-many-parasites"), xeno, xeno);
            return;
        }

        if (_mind.TryGetMind(target, out _, out _))
        {
            _popup.PopupEntity(Loc.GetString("rmc-xeno-egg-awake-child", ("parasite", target)), xeno, xeno);
            return;
        }

        AddParasite(target, xeno);

        var msg = Loc.GetString("cm-xeno-throw-parasite-stash-parasite", ("cur_parasites", xeno.Comp.CurParasites), ("max_parasites", xeno.Comp.MaxParasites));
        _popup.PopupEntity(msg, xeno, xeno);

        args.Handled = true;
    }

    private void OnXenoEvolveDoAfter(Entity<XenoParasiteThrowerComponent> xeno, ref XenoEvolutionDoAfterEvent args)
    {
        DropAllStoredParasites(xeno);
    }

    private void OnXenoDevolveDoAfter(Entity<XenoParasiteThrowerComponent> xeno, ref XenoDevolveBuiMsg args)
    {
        DropAllStoredParasites(xeno);
    }

    private void OnMobStateChanged(Entity<XenoParasiteThrowerComponent> xeno, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        DropAllStoredParasites(xeno, 0.75f);
    }

    private bool DropAllStoredParasites(Entity<XenoParasiteThrowerComponent> xeno, float chance = 1.0f)
    {
        TryComp(xeno, out XenoComponent? _);

        if (chance != 1.0 && xeno.Comp.CurParasites > 0)
            _popup.PopupEntity(Loc.GetString("rmc-xeno-parasite-carrier-death", ("xeno", xeno)), xeno, PopupType.MediumCaution);

        var hive = _hive.GetHive(xeno.Owner);

        for (var i = 0; i < xeno.Comp.CurParasites; ++i)
        {
            if (chance != 1.0 && !_random.Prob(chance))
                continue;
            var newParasite = Spawn(xeno.Comp.ParasitePrototype);
            _hive.SetHive(newParasite, hive);
            _transform.DropNextTo(newParasite, xeno.Owner);
            //So they don't eat eachother before they gloriously fly into the sunset
            _stun.TryStun(newParasite, xeno.Comp.ThrownParasiteStunDuration, true);
            _throw.TryThrow(newParasite, _random.NextAngle().RotateVec(Vector2.One) * _random.NextFloat(0.15f, 0.7f), 3);
        }

        xeno.Comp.CurParasites = 0; // Just in case

        UpdateParasiteClingers(xeno);
        return true;
    }

    /// <summary>
    /// Delete the parasite provided, increment XenoParasiteThrower Component's CurParasites
    /// Does not peform any checks.
    /// </summary>
    private void AddParasite(EntityUid parasite, Entity<XenoParasiteThrowerComponent> xeno)
    {
        xeno.Comp.CurParasites++;

        UpdateParasiteClingers(xeno);

        QueueDel(parasite);
    }

    /// <summary>
    /// Spawn a parasite, decrement XenoParasiteThrower Component's CurParasites, and return the new parasite.
    /// Does not peform any checks.
    /// </summary>
    private EntityUid? RemoveParasite(Entity<XenoParasiteThrowerComponent> xeno)
    {
        xeno.Comp.CurParasites--;

        UpdateParasiteClingers(xeno);

        return Spawn(xeno.Comp.ParasitePrototype);
    }

    public EntityUid? TryRemoveGhostParasite(Entity<XenoParasiteThrowerComponent> xeno, out string message)
    {
        message = "";
        if (xeno.Comp.CurParasites <= 0)
        {
            message = Loc.GetString("rmc-xeno-parasite-ghost-carrier-none", ("xeno", xeno));
            return null;
        }

        if (xeno.Comp.ReservedParasites >= xeno.Comp.CurParasites)
        {
            message = Loc.GetString("rmc-xeno-parasite-ghost-carrier-reserved", ("xeno", xeno));
            return null;
        }

        if (_mobState.IsDead(xeno))
        {
            message = Loc.GetString("rmc-xeno-parasite-ghost-carrier-dead", ("xeno", xeno));
            return null;
        }

        var para = RemoveParasite(xeno);
        if (para == null)
            return null;

        _hive.SetSameHive(xeno.Owner, para.Value);
        _rmcObstacleSlamming.MakeImmune(para.Value);
        _transform.DropNextTo(para.Value, xeno.Owner);
        // Small throw

        _throw.TryThrow(para.Value, _random.NextAngle().RotateVec(Vector2.One), 3);

        return para;
    }
}

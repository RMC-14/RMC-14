using System.Linq;
using System.Numerics;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Shared._RMC14.Atmos;
using Content.Shared._RMC14.Damage.ObstacleSlamming;
using Content.Shared._RMC14.Xenonids;
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

namespace Content.Server._RMC14.Xenonids.Parasite;

public sealed partial class XenoParasiteThrowerSystem : SharedXenoParasiteThrowerSystem
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
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly RMCObstacleSlammingSystem _rmcObstacleSlamming = default!;

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

            foreach (var possibleParasite in clickedEntities)
            {
                if (_mobState.IsDead(possibleParasite))
                    continue;

                if (!HasComp<XenoParasiteComponent>(possibleParasite))
                    continue;

                if (!HasComp<ParasiteAIComponent>(possibleParasite))
                    continue;

                tileHasParasites = true;

                if (xeno.Comp.CurParasites >= xeno.Comp.MaxParasites)
                {
                    _popup.PopupEntity(Loc.GetString("cm-xeno-throw-parasite-too-many-parasites"), xeno, xeno);
                    return;
                }

                AddParasite(possibleParasite, xeno);
            }

            if (tileHasParasites)
            {
                var stashMsg = Loc.GetString("cm-xeno-throw-parasite-stash-parasite", ("cur_parasites", xeno.Comp.CurParasites), ("max_parasites", xeno.Comp.MaxParasites));
                _popup.PopupEntity(stashMsg, xeno, xeno);
                return;
            }
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

    private void UpdateParasiteClingers(Entity<XenoParasiteThrowerComponent> xeno)
    {
        var parasiteNumber = Math.Min(Math.Ceiling((((double)xeno.Comp.CurParasites / xeno.Comp.MaxParasites) * xeno.Comp.NumPositions)), xeno.Comp.NumPositions);

        var overlayNumbers = xeno.Comp.VisiblePositions.Count(position => position == true);

        if (overlayNumbers > parasiteNumber)
        {
            var visibleIndexes = GetVisualIndexes(xeno.Comp.VisiblePositions, true);
            for (var i = 0; i < overlayNumbers - parasiteNumber; i++)
            {
                var index = _random.PickAndTake(visibleIndexes);
                xeno.Comp.VisiblePositions[index] = false;
            }
        }
        else
        {
            var invisibleIndexes = GetVisualIndexes(xeno.Comp.VisiblePositions, false);
            for (var i = 0; i < parasiteNumber - overlayNumbers; i++)
            {
                var index = _random.PickAndTake(invisibleIndexes);
                xeno.Comp.VisiblePositions[index] = true;
            }
        }

        Dirty(xeno);

        //Need to clone the array for it to dirty properly
        _appearance.SetData(xeno, ParasiteOverlayVisuals.States, xeno.Comp.VisiblePositions.Clone());
    }

    private List<int> GetVisualIndexes(bool[] bools, bool visible)
    {
        List<int> visualIndexes = new();
        for (int i = 0; i < bools.Length; i++)
        {
            if (bools[i] == visible)
                visualIndexes.Add(i);
        }
        return visualIndexes;
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
        _transform.DropNextTo(para.Value, xeno.Owner);
        // Small throw
        _throw.TryThrow(para.Value, _random.NextAngle().RotateVec(Vector2.One), 3);

        return para;
    }
}

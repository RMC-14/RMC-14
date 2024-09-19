using Content.Server.Ghost.Roles;
using Content.Server.Ghost.Roles.Components;
using Content.Server.Hands.Systems;
using Content.Server.Popups;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared._RMC14.Xenonids.Parasite;
using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using Content.Shared.Actions;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Stunnable;
using Content.Shared.Throwing;
using Robust.Shared.Random;
using Robust.Shared.Utility;


namespace Content.Server._RMC14.Xenonids.Projectile.Parasite;

public sealed partial class XenoParasiteThrowerSystem : SharedXenoParasiteThrowerSystem
{
    [Dependency] private readonly SharedActionsSystem _action = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly SharedInteractionSystem _interact = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ThrowingSystem _throw = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly EntityManager _entities = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();


        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoThrowParasiteActionEvent>(OnToggleParasiteThrow);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoReserveParasiteActionEvent>(OnSetReserve);

        SubscribeLocalEvent<XenoParasiteThrowerComponent, UserActivateInWorldEvent>(OnXenoParasiteThrowerUseInHand);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoEvolutionDoAfterEvent>(OnXenoEvolveDoAfter);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoDevolveBuiMsg>(OnXenoDevolveDoAfter);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, MobStateChangedEvent>(OnDeathMobStateChanged);
        SubscribeLocalEvent<XenoParasiteThrowerComponent, XenoChangeParasiteReserveEvent>(OnChangeParasiteReserve);
    }

    private void OnToggleParasiteThrow(Entity<XenoParasiteThrowerComponent> xeno, ref XenoThrowParasiteActionEvent args)
    {
        var (ent, comp) = xeno;

        var target = args.Target;

        args.Handled = true;

        // If none of the entities on the selected, in-range tile are parasites, try to pull out a
        // parasite OR try to throw a held parasite
        if (_interact.InRangeUnobstructed(ent, target))
        {
            var clickedEntities = _lookup.GetEntitiesIntersecting(target);
            var tileHasParasites = false;

            foreach (var possibleParasite in clickedEntities)
            {
                if (_mobState.IsDead(possibleParasite))
                {
                    continue;
                }

                if (!HasComp<XenoParasiteComponent>(possibleParasite))
                {
                    continue;
                }

                tileHasParasites = true;

                if (comp.CurParasites >= comp.MaxParasites)
                {
                    _popup.PopupEntity(Loc.GetString("cm-xeno-throw-parasite-too-many-parasites"), ent, ent);
                    return;
                }

                AddParasite(possibleParasite, xeno);
            }

            if (tileHasParasites)
            {
                var stashMsg = Loc.GetString("cm-xeno-throw-parasite-stash-parasite", ("cur_parasites", comp.CurParasites), ("max_parasites", comp.MaxParasites));
                _popup.PopupEntity(stashMsg, ent, ent);
                return;
            }
        }

        if (_hands.GetActiveItem(ent) is EntityUid heldEntity &&
            HasComp<XenoParasiteComponent>(heldEntity) &&
            !_mobState.IsDead(heldEntity))
        {
            _hands.ThrowHeldItem(xeno.Owner, target);

            _stun.TryStun(heldEntity, comp.ThrownParasiteStunDuration, true);
            return;
        }

        if (comp.CurParasites == 0)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-throw-parasite-no-parasites"), ent, ent);
            return;
        }

        if (RemoveParasite(xeno) is not EntityUid newParasite)
        {
            return;
        }
        _hands.TryPickupAnyHand(ent, newParasite);

        var msg = Loc.GetString("cm-xeno-throw-parasite-unstash-parasite", ("cur_parasites", comp.CurParasites), ("max_parasites", comp.MaxParasites));
        _popup.PopupEntity(msg, ent, ent);
    }

    private void OnSetReserve(Entity<XenoParasiteThrowerComponent> xeno, ref XenoReserveParasiteActionEvent args)
    {
        if (args.Handled)
        {
            return;
        }

        _ui.OpenUi(args.Action, XenoReserveParasiteChangeUIKey.Key, xeno.Owner);

        args.Handled = true;
    }

    private void OnXenoParasiteThrowerUseInHand(Entity<XenoParasiteThrowerComponent> xeno, ref UserActivateInWorldEvent args)
    {
        var (ent, comp) = xeno;
        var target = args.Target;

        if (!HasComp<XenoParasiteComponent>(target))
        {
            return;
        }

        if (_mobState.IsDead(target))
        {
            return;
        }

        if (args.Handled)
        {
            return;
        }

        if (comp.CurParasites >= comp.MaxParasites)
        {
            _popup.PopupEntity(Loc.GetString("cm-xeno-throw-parasite-too-many-parasites"), ent, ent);
            return;
        }

        AddParasite(target, xeno);

        var msg = Loc.GetString("cm-xeno-throw-parasite-stash-parasite", ("cur_parasites", comp.CurParasites), ("max_parasites", comp.MaxParasites));
        _popup.PopupEntity(msg, ent, ent);
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

    private void OnDeathMobStateChanged(Entity<XenoParasiteThrowerComponent> xeno, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;
        DropAllStoredParasites(xeno);
    }

    private void OnChangeParasiteReserve(Entity<XenoParasiteThrowerComponent> xeno, ref XenoChangeParasiteReserveEvent args)
    {
        xeno.Comp.ReservedParasites = args.NewReserve;
    }

    private bool DropAllStoredParasites(Entity<XenoParasiteThrowerComponent> xeno)
    {
        for (var i = 0; i < xeno.Comp.CurParasites; ++i)
        {
            var newParasite = Spawn(xeno.Comp.ParasitePrototype);
            _transform.DropNextTo(newParasite, xeno.Owner);
        }
        return true;
    }

    /// <summary>
    /// Delete the parasite provided, increment XenoParasiteThrower Component's CurParasites
    /// Does not peform any checks.
    /// </summary>
    private void AddParasite(EntityUid parasite, Entity<XenoParasiteThrowerComponent> xeno)
    {
        xeno.Comp.CurParasites++;

        QueueDel(parasite);
    }

    /// <summary>
    /// Spawn a parasite, decrement XenoParasiteThrower Component's CurParasites, and return the new parasite.
    /// Does not peform any checks.
    /// </summary>
    private EntityUid? RemoveParasite(Entity<XenoParasiteThrowerComponent> xeno)
    {
        xeno.Comp.CurParasites--;

        return Spawn(xeno.Comp.ParasitePrototype);
    }
}

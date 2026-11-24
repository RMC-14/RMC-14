using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Armor;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared._RMC14.Xenonids.Projectile.Spit.Dissolver;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Robust.Shared.Utility;
using static Content.Shared._RMC14.Dropship.Weapon.DropshipTerminalWeaponsComponent;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit;

public sealed class XenoDissolverBombardSystem : EntitySystem
{
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafter = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoProjectileSystem _xenoProjectile = default!;
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly CMArmorSystem _armor = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<XenoDissolverBombardComponent, XenoDissolverStoreAcidActionEvent>(OnXenoDissolverStoreAcidActionEvent);

        SubscribeLocalEvent<XenoDissolverBombardComponent, XenoDissolverBombardActionEvent>(OnXenoDissolverBombardActionEvent);
        SubscribeLocalEvent<XenoDissolverBombardComponent, XenoDissolverBombardDoAfterEvent>(OnXenoDissolverBombardDoAfterEvent);

        SubscribeLocalEvent<XenoDissolverBombardComponent, CMGetArmorEvent>(OnXenoDissolverBombardGetArmor);
    }

    private void OnXenoDissolverStoreAcidActionEvent(Entity<XenoDissolverBombardComponent> xeno, ref XenoDissolverStoreAcidActionEvent args)
    {
        if (args.Handled)
            return;

        //Can't use during windups
        if (xeno.Comp.BombardDoafter != null || xeno.Comp.StoredAcid >= xeno.Comp.MaxStoredAcid)
            return;

        if (!_rmcActions.TryUseAction(args))
            return;

        args.Handled = true;

        ModifyStoredAcid(xeno, 1);
    }

    private void OnXenoDissolverBombardActionEvent(Entity<XenoDissolverBombardComponent> xeno, ref XenoDissolverBombardActionEvent args)
    {
        if (!_rmcActions.TryUseAction(args))
            return;

        if (!_xenoPlasma.HasPlasmaPopup(xeno.Owner, args.PlasmaCost))
            return;

        // Do retargeting if doafter is currently running
        // TODO RMC14 allow changing target on mouse drag
        if (xeno.Comp.BombardDoafter != null)
        {
            xeno.Comp.TargetCoordinates = args.Target;
            xeno.Comp.TargetEnt = args.Entity;
            return;
        }

        var spit = args.BaseSpit;
        var windup = args.Windup;

        if (args.SpitTypePerChargeLevel.Length > 0)
            spit = args.SpitTypePerChargeLevel[xeno.Comp.StoredAcid];

        if (args.WindupReductionPerStack != TimeSpan.Zero)
            windup -= args.WindupReductionPerStack * xeno.Comp.StoredAcid;

        xeno.Comp.TargetCoordinates = args.Target;
        xeno.Comp.TargetEnt = args.Entity;

        var ev = new XenoDissolverBombardDoAfterEvent(spit, args.ExtraProjectiles, args.Scatter, args.Speed, args.PlasmaCost, args.MaxDistance);
        var doAfter = new DoAfterArgs(EntityManager, xeno, windup, ev, xeno) { BreakOnMove = true, RootEntity = true };
        _doafter.TryStartDoAfter(doAfter, out xeno.Comp.BombardDoafter);
        Dirty(xeno);
    }

    private void OnXenoDissolverBombardDoAfterEvent(Entity<XenoDissolverBombardComponent> xeno, ref XenoDissolverBombardDoAfterEvent args)
    {
        xeno.Comp.BombardDoafter = null;
        Dirty(xeno);

        if (args.Handled || args.Cancelled || xeno.Comp.TargetCoordinates == null)
            return;

        args.Handled = true;

        float? stopDistance = null;

        if (args.MaxDistance != null)
        {
            var targetMap = _transform.ToMapCoordinates(xeno.Comp.TargetCoordinates.Value);
            var origin = _transform.GetMapCoordinates(xeno);
            if (origin.MapId != targetMap.MapId)
                return;

            var direction = targetMap.Position - origin.Position;
            stopDistance = Math.Min(args.MaxDistance.Value, direction.Length());
        }

        _xenoProjectile.TryShoot(xeno, xeno.Comp.TargetCoordinates.Value, args.PlasmaCost, args.SpitId, xeno.Comp.Sound,
            1 + args.ExtraProjectiles, args.Scatter, args.Speed, stopDistance, xeno.Comp.TargetEnt, predicted: (stopDistance == null));

        StoredAcidCooldowns(xeno);
    }

    private void StoredAcidCooldowns(Entity<XenoDissolverBombardComponent> xeno)
    {
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoDissolverBombardActionEvent>(xeno))
        {
            _actions.SetCooldown(action.AsNullable(), xeno.Comp.StoredAcid > 0 ? xeno.Comp.ReducedCooldown : xeno.Comp.BaseCooldown);
        }

        foreach (var action in _rmcActions.GetActionsWithEvent<XenoDissolverStoreAcidActionEvent>(xeno))
        {
            _actions.SetCooldown(action.AsNullable(), xeno.Comp.StoredAcidCooldown);
        }

        ModifyStoredAcid(xeno, -1);
    }

    private void ModifyStoredAcid(Entity<XenoDissolverBombardComponent> xeno, int amount)
    {
        xeno.Comp.StoredAcid = Math.Clamp(xeno.Comp.StoredAcid + amount, 0, xeno.Comp.MaxStoredAcid);
        _armor.UpdateArmorValue(xeno.Owner);
        foreach (var action in _rmcActions.GetActionsWithEvent<XenoDissolverStoreAcidActionEvent>(xeno))
        {
            _actions.SetIcon(action.AsNullable(), new SpriteSpecifier.Rsi(xeno.Comp.AcidIconResPath, xeno.Comp.AcidIconStatePrefix + xeno.Comp.StoredAcid));
        }
        Dirty(xeno);
    }

    private void OnXenoDissolverBombardGetArmor(Entity<XenoDissolverBombardComponent> xeno, ref CMGetArmorEvent args)
    {
        args.XenoArmor -= xeno.Comp.StoredAcid * xeno.Comp.ArmorReductionPerStoredAcid;
    }
}

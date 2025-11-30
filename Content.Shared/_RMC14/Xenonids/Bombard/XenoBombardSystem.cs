using System.Numerics;
using Content.Shared._RMC14.Actions;
using Content.Shared._RMC14.Projectiles;
using Content.Shared._RMC14.Xenonids.GasToggle;
using Content.Shared._RMC14.Xenonids.Hive;
using Content.Shared._RMC14.Xenonids.Plasma;
using Content.Shared.Actions.Components;
using Content.Shared.DoAfter;
using Content.Shared.Popups;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Bombard;

public sealed class XenoBombardSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;
    [Dependency] private readonly SharedXenoHiveSystem _hive = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCActionsSystem _rmcActions = default!;
    [Dependency] private readonly RMCProjectileSystem _rmcProjectile = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly XenoPlasmaSystem _xenoPlasma = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<XenoBombardComponent, XenoBombardActionEvent>(OnBombard);
        SubscribeLocalEvent<XenoBombardComponent, DoAfterAttemptEvent<XenoBombardDoAfterEvent>>(OnBombardDoAfterAttempt);
        SubscribeLocalEvent<XenoBombardComponent, XenoBombardDoAfterEvent>(OnBombardDoAfter);
        SubscribeLocalEvent<XenoBombardComponent, XenoGasToggleActionEvent>(OnToggleType);
    }

    private void OnBombard(Entity<XenoBombardComponent> ent, ref XenoBombardActionEvent args)
    {
        var source = _transform.GetMapCoordinates(ent);
        var target = _transform.ToMapCoordinates(args.Target);
        if (source.MapId != target.MapId)
            return;

        args.Handled = true;

        if (!_xenoPlasma.HasPlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
            return;

        var direction = target.Position - source.Position;
        if (direction.Length() > ent.Comp.Range)
            target = source.Offset(direction.Normalized() * ent.Comp.Range);

        _audio.PlayPredicted(ent.Comp.PrepareSound, ent, ent);

        var ev = new XenoBombardDoAfterEvent { Coordinates = target, };
        var doAfter = new DoAfterArgs(EntityManager, ent, ent.Comp.Delay, ev, ent, args.Action) { BreakOnMove = true, RootEntity = true };
        if (_doAfter.TryStartDoAfter(doAfter))
        {
            _rmcActions.DisableSharedCooldownEvents(args.Action.Owner, ent);
            var selfMessage = Loc.GetString("rmc-glob-start-self");
            _popup.PopupClient(selfMessage, ent, ent);

            var othersMessage = Loc.GetString("rmc-glob-start-others", ("user", ent));
            _popup.PopupEntity(othersMessage, ent, Filter.PvsExcept(ent), true, PopupType.MediumCaution);
        }
    }

    private void OnBombardDoAfterAttempt(Entity<XenoBombardComponent> ent, ref DoAfterAttemptEvent<XenoBombardDoAfterEvent> args)
    {
        if (args.Event.Target is { } action &&
            HasComp<InstantActionComponent>(action) &&
            TryComp(action, out ActionComponent? actionComponent) &&
            !actionComponent.Enabled)
        {
            _rmcActions.EnableSharedCooldownEvents(action, ent);
            args.Cancel();
        }
    }

    private void OnBombardDoAfter(Entity<XenoBombardComponent> ent, ref XenoBombardDoAfterEvent args)
    {
        if (args.Target is not { } action)
            return;
        _rmcActions.EnableSharedCooldownEvents(action, ent);
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        if (!_xenoPlasma.TryRemovePlasmaPopup(ent.Owner, ent.Comp.PlasmaCost))
            return;

        if (_net.IsClient)
            return;

        var source = _transform.GetMapCoordinates(ent);
        if (source.MapId != args.Coordinates.MapId)
            return;

        var direction = args.Coordinates.Position - source.Position;
        var projectile = Spawn(ent.Comp.Projectile, source);
        _hive.SetSameHive(ent.Owner, projectile);

        var max = EnsureComp<ProjectileMaxRangeComponent>(projectile);
        _rmcProjectile.SetMaxRange((projectile, max), direction.Length());

        _gun.ShootProjectile(projectile, direction, Vector2.Zero, ent, ent, speed: 7.5f);
        _audio.PlayEntity(ent.Comp.ShootSound, ent, ent);

        _rmcActions.ActivateSharedCooldown(action, ent);

        var selfMessage = Loc.GetString("rmc-glob-shoot-self");
        _popup.PopupClient(selfMessage, ent, ent);

        var othersMessage = Loc.GetString("rmc-glob-shoot-others", ("user", ent));
        _popup.PopupEntity(othersMessage, ent, Filter.PvsExcept(ent), true, PopupType.MediumCaution);
    }

    private void OnToggleType(Entity<XenoBombardComponent> ent, ref XenoGasToggleActionEvent args)
    {
        if (ent.Comp.Projectiles.Length == 0)
            return;

        var index = Array.IndexOf(ent.Comp.Projectiles, ent.Comp.Projectile);
        if (index == -1 || index >= ent.Comp.Projectiles.Length - 1)
            index = 0;
        else
            index++;

        ent.Comp.Projectile = ent.Comp.Projectiles[index];
        Dirty(ent);
    }
}

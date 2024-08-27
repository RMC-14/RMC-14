using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Rules;
using Content.Shared.Construction.Components;
using Content.Shared.Coordinates;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Mortar;

public abstract class SharedMortarSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<TransformComponent> _transformQuery;

    public override void Initialize()
    {
        _transformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<MortarComponent, UseInHandEvent>(OnMortarUseInHand, before: [typeof(ActivatableUISystem)]);
        SubscribeLocalEvent<MortarComponent, DeployMortarDoAfterEvent>(OnMortarDeployDoAfter);
        SubscribeLocalEvent<MortarComponent, TargetMortarDoAfterEvent>(OnMortarTargetDoAfter);
        SubscribeLocalEvent<MortarComponent, DialMortarDoAfterEvent>(OnMortarDialDoAfter);
        SubscribeLocalEvent<MortarComponent, InteractUsingEvent>(OnMortarInteractUsing);
        SubscribeLocalEvent<MortarComponent, LoadMortarShellDoAfterEvent>(OnMortarLoadDoAfter);
        SubscribeLocalEvent<MortarComponent, UnanchorAttemptEvent>(OnMortarUnanchorAttempt);
        SubscribeLocalEvent<MortarComponent, AnchorStateChangedEvent>(OnMortarAnchorStateChanged);
        SubscribeLocalEvent<MortarComponent, ExaminedEvent>(OnMortarExamined);
        SubscribeLocalEvent<MortarComponent, ActivatableUIOpenAttemptEvent>(OnMortarActivatableUIOpenAttempt);

        Subs.BuiEvents<MortarComponent>(MortarUiKey.Key,
            subs =>
            {
                subs.Event<MortarTargetBuiMsg>(OnMortarTargetBui);
                subs.Event<MortarDialBuiMsg>(OnMortarDialBui);
            });
    }

    private void OnMortarUseInHand(Entity<MortarComponent> mortar, ref UseInHandEvent args)
    {
        args.Handled = true;
        DeployMortar(mortar, args.User);
    }

    private void OnMortarDeployDoAfter(Entity<MortarComponent> mortar, ref DeployMortarDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;
        if (mortar.Comp.Deployed)
            return;

        mortar.Comp.Deployed = true;
        Dirty(mortar);

        _appearance.SetData(mortar, MortarVisualLayers.State, MortarVisuals.Deployed);

        var xform = Transform(mortar);
        var coordinates = _transform.GetMoverCoordinates(mortar, xform);
        _transform.SetCoordinates(mortar, coordinates);
        _transform.AnchorEntity((mortar, xform));

        if (!_rmcPlanet.IsOnPlanet(coordinates))
            _popup.PopupClient(Loc.GetString("rmc-mortar-deploy-end-not-planet"), user, user, PopupType.MediumCaution);
    }

    private void OnMortarTargetDoAfter(Entity<MortarComponent> mortar, ref TargetMortarDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var user = args.User;
        var selfMsg = Loc.GetString("rmc-mortar-target-finish-self", ("mortar", mortar));
        var othersMsg = Loc.GetString("rmc-mortar-target-finish-others", ("user", user), ("mortar", mortar));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        if (_net.IsClient)
            return;

        var target = args.Vector;
        var position = _transform.GetMapCoordinates(mortar).Position;
        var offset = target;
        if (_rmcPlanet.TryGetOffset(mortar.Owner.ToCoordinates(), out var planetOffset))
            offset -= planetOffset;

        mortar.Comp.Target = target;

        var xOffset = (int) Math.Floor(Math.Abs(offset.X - position.X));
        var yOffset = (int) Math.Floor(Math.Abs(offset.Y - position.Y));
        mortar.Comp.Offset = (_random.Next(-xOffset, xOffset + 1), _random.Next(-yOffset, yOffset + 1));

        Dirty(mortar);
    }

    private void OnMortarDialDoAfter(Entity<MortarComponent> mortar, ref DialMortarDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var user = args.User;
        var selfMsg = Loc.GetString("rmc-mortar-dial-finish-self", ("mortar", mortar));
        var othersMsg = Loc.GetString("rmc-mortar-dial-finish-others", ("user", user), ("mortar", mortar));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
    }

    private void OnMortarInteractUsing(Entity<MortarComponent> mortar, ref InteractUsingEvent args)
    {
        var used = args.Used;
        if (!TryComp(used, out MortarShellComponent? shell))
            return;

        args.Handled = true;
        var user = args.User;
        if (!HasSkillPopup(mortar, user, true))
            return;

        if (!CanLoadPopup(mortar, (used, shell), user, out _, out _))
            return;

        var ev = new LoadMortarShellDoAfterEvent();
        var doAfter = new DoAfterArgs(EntityManager, user, shell.LoadDelay, ev, mortar, mortar, used)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        _doAfter.TryStartDoAfter(doAfter);
    }

    private void OnMortarLoadDoAfter(Entity<MortarComponent> mortar, ref LoadMortarShellDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled || args.Used is not { } shellId)
            return;

        if (!TryComp(shellId, out MortarShellComponent? shell))
            return;

        if (!mortar.Comp.Deployed)
            return;

        if (HasComp<ActiveMortarShellComponent>(shellId))
            return;

        if (!CanLoadPopup(mortar, (shellId, shell), user, out var travelTime, out var coordinates))
            return;

        var container = _container.EnsureContainer<Container>(mortar, mortar.Comp.ContainerId);
        if (!_container.Insert(shellId, container))
            return;

        var time = _timing.CurTime;
        var active = new ActiveMortarShellComponent
        {
            Coordinates = coordinates,
            WarnAt = time + travelTime,
            ImpactWarnAt = time + travelTime + shell.ImpactWarningDelay,
            LandAt = time + travelTime + shell.ImpactDelay,
        };

        AddComp(shellId, active, true);

        var msg = Loc.GetString("rmc-mortar-shell-load-finish-self", ("mortar", mortar), ("shell", shellId));
        _popup.PopupClient(msg, user, user);

        msg = Loc.GetString("rmc-mortar-shell-fire", ("mortar", mortar));
        _popup.PopupClient(msg, mortar, user, PopupType.MediumCaution);
    }

    private void OnMortarUnanchorAttempt(Entity<MortarComponent> mortar, ref UnanchorAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasSkillPopup(mortar, args.User, true))
            args.Cancel();
    }

    private void OnMortarAnchorStateChanged(Entity<MortarComponent> mortar, ref AnchorStateChangedEvent args)
    {
        if (args.Anchored)
            return;

        mortar.Comp.Deployed = false;
        Dirty(mortar);
        _appearance.SetData(mortar, MortarVisualLayers.State, MortarVisuals.Item);
    }

    private void OnMortarExamined(Entity<MortarComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(MortarComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-mortar-less-accurate-with-range"));
        }
    }

    private void OnMortarActivatableUIOpenAttempt(Entity<MortarComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Deployed)
            args.Cancel();
    }

    private void OnMortarTargetBui(Entity<MortarComponent> mortar, ref MortarTargetBuiMsg args)
    {
        Cap(ref args.Target.X, mortar.Comp.MaxTarget);
        Cap(ref args.Target.Y, mortar.Comp.MaxTarget);

        var user = args.Actor;
        var ev = new TargetMortarDoAfterEvent(args.Target);
        var doAfter = new DoAfterArgs(EntityManager, user, mortar.Comp.TargetDelay, ev, mortar)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);

        var selfMsg = Loc.GetString("rmc-mortar-target-start-self", ("mortar", mortar));
        var othersMsg = Loc.GetString("rmc-mortar-target-start-others", ("user", user), ("mortar", mortar));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
    }

    private void OnMortarDialBui(Entity<MortarComponent> mortar, ref MortarDialBuiMsg args)
    {
        Cap(ref args.Target.X, mortar.Comp.MaxDial);
        Cap(ref args.Target.Y, mortar.Comp.MaxDial);

        var user = args.Actor;
        var ev = new DialMortarDoAfterEvent(args.Target);
        var doAfter = new DoAfterArgs(EntityManager, user, mortar.Comp.TargetDelay, ev, mortar)
        {
            BreakOnMove = true,
        };

        _doAfter.TryStartDoAfter(doAfter);

        var selfMsg = Loc.GetString("rmc-mortar-dial-start-self", ("mortar", mortar));
        var othersMsg = Loc.GetString("rmc-mortar-dial-start-others", ("user", user), ("mortar", mortar));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
    }

    private void DeployMortar(Entity<MortarComponent> mortar, EntityUid user)
    {
        if (mortar.Comp.Deployed)
            return;

        if (!HasSkillPopup(mortar, user, true))
            return;

        var ev = new DeployMortarDoAfterEvent();
        var args = new DoAfterArgs(EntityManager, user, mortar.Comp.DeployDelay, ev, mortar)
        {
            BreakOnMove = true,
            BreakOnHandChange = true,
        };

        if (_doAfter.TryStartDoAfter(args))
            _popup.PopupClient(Loc.GetString("rmc-mortar-deploy-start", ("mortar", mortar)), user, user);
    }

    protected bool HasSkillPopup(Entity<MortarComponent> mortar, EntityUid user, bool predicted)
    {
        if (_skills.HasSkills(user, mortar.Comp.Skill))
            return true;

        var msg = Loc.GetString("rmc-skills-no-training", ("target", mortar));
        if (predicted)
            _popup.PopupClient(msg, user, user, PopupType.SmallCaution);
        else
            _popup.PopupEntity(msg, user, user, PopupType.SmallCaution);

        return false;
    }

    protected virtual bool CanLoadPopup(
        Entity<MortarComponent> mortar,
        Entity<MortarShellComponent> shell,
        EntityUid user,
        out TimeSpan travelTime,
        out EntityCoordinates coordinates)
    {
        travelTime = default;
        coordinates = default;
        return false;
    }

    private void PopupWarning(MapCoordinates coordinates, float range, LocId warning, LocId warningAbove)
    {
        foreach (var session in _player.NetworkedSessions)
        {
            if (session.AttachedEntity is not { } recipient ||
                !_transformQuery.TryComp(recipient, out var xform) ||
                xform.MapID != coordinates.MapId)
            {
                continue;
            }

            var sessionCoordinates = _transform.GetMapCoordinates(xform);
            var distanceVec = (coordinates.Position - sessionCoordinates.Position);
            var distance = distanceVec.Length();
            if (distance > range)
                continue;

            var direction = distanceVec.GetDir().ToString().ToUpperInvariant();
            var msg = distance < 1
                ? Loc.GetString(warningAbove)
                : Loc.GetString(warning, ("direction", direction));
            _popup.PopupClient(msg, recipient, recipient, PopupType.LargeCaution);
        }
    }

    private void Cap(ref int value, int at)
    {
        at = Math.Abs(at);
        if (value > at)
            value = at;
        else if (value < -at)
            value = -at;
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient)
            return;

        var time = _timing.CurTime;
        var shells = EntityQueryEnumerator<ActiveMortarShellComponent>();
        while (shells.MoveNext(out var uid, out var active))
        {
            if (!active.Warned && time >= active.WarnAt)
            {
                active.Warned = true;
                var coordinates = _transform.ToMapCoordinates(active.Coordinates);
                PopupWarning(coordinates,
                    active.WarnRange,
                    "rmc-mortar-shell-warning",
                    "rmc-mortar-shell-warning-above");
            }

            if (!active.ImpactWarned && time >= active.ImpactWarnAt)
            {
                active.ImpactWarned = true;
                var coordinates = _transform.ToMapCoordinates(active.Coordinates);
                PopupWarning(coordinates,
                    active.WarnRange,
                    "rmc-mortar-shell-impact-warning",
                    "rmc-mortar-shell-impact-warning-above");
            }

            if (time >= active.LandAt)
            {
                _transform.SetCoordinates(uid, active.Coordinates);
                _rmcExplosion.TriggerExplosive(uid);

                if (!EntityManager.IsQueuedForDeletion(uid))
                    QueueDel(uid);
            }
        }
    }
}

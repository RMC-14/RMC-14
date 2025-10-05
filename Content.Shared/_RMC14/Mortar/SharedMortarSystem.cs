using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Camera;
using Content.Shared._RMC14.Chat;
using Content.Shared._RMC14.Explosion;
using Content.Shared._RMC14.Extensions;
using Content.Shared._RMC14.Map;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Rangefinder;
using Content.Shared._RMC14.Rules;
using Content.Shared._RMC14.Xenonids;
using Content.Shared.Administration.Logs;
using Content.Shared.Chat;
using Content.Shared.Construction.Components;
using Content.Shared.Coordinates;
using Content.Shared.Damage;
using Content.Shared.Database;
using Content.Shared.Destructible;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Mortar;

public abstract class SharedMortarSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogs = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly FixtureSystem _fixture = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedCMChatSystem _rmcChat = default!;
    [Dependency] private readonly SharedRMCExplosionSystem _rmcExplosion = default!;
    [Dependency] private readonly RMCMapSystem _rmcMap = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

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
        SubscribeLocalEvent<MortarComponent, CombatModeShouldHandInteractEvent>(OnMortarShouldInteract);
        SubscribeLocalEvent<MortarComponent, DestructionEventArgs>(OnMortarDestruction);
        SubscribeLocalEvent<MortarComponent, BeforeDamageChangedEvent>(OnMortarBeforeDamageChanged);
        SubscribeLocalEvent<MortarComponent, LinkMortarLaserDesignatorDoAfterEvent>(OnMortarLinkLaserDesignatorDoAfter);
        SubscribeLocalEvent<MortarComponent, GetVerbsEvent<AlternativeVerb>>(OnGetMortarVerbs);
        SubscribeLocalEvent<MortarComponent, MortarLaserTargetUpdateDoAfterEvent>(OnMortarLaserTargetUpdateDoAfter);
        SubscribeLocalEvent<MortarComponent, DoAfterAttemptEvent<MortarLaserTargetUpdateDoAfterEvent>>(OnMortarLaserTargetUpdateAttempt);

        SubscribeLocalEvent<ActivateMortarShellOnSpawnComponent, MapInitEvent>(OnMortarExplosionOnSpawn);

        SubscribeLocalEvent<MortarCameraShellComponent, MortarShellLandEvent>(OnMortarCameraShellLand);
        SubscribeLocalEvent<ActiveLaserDesignatorComponent, ComponentShutdown>(OnActiveLaserDesignatorShutdown);
        SubscribeLocalEvent<RangefinderComponent, ComponentShutdown>(OnRangefinderShutdown);

        Subs.BuiEvents<MortarComponent>(MortarUiKey.Key,
            subs =>
            {
                subs.Event<MortarTargetBuiMsg>(OnMortarTargetBui);
                subs.Event<MortarDialBuiMsg>(OnMortarDialBui);
                subs.Event<MortarViewCamerasMsg>(OnMortarViewCameras);
            });
    }

    private void OnMortarBeforeDamageChanged(Entity<MortarComponent> ent, ref BeforeDamageChangedEvent args)
    {
        if (!ent.Comp.Deployed) // cannot destroy in item form
            args.Cancelled = true;
    }

    private void OnMortarDestruction(Entity<MortarComponent> mortar, ref DestructionEventArgs args)
    {
        if (!mortar.Comp.Deployed || _net.IsClient)
            return;

        SpawnAtPosition(mortar.Comp.Drop, mortar.Owner.ToCoordinates());
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

        if (!CanDeployPopup(mortar, user))
            return;

        mortar.Comp.Deployed = true;
        Dirty(mortar);

        if (_fixture.GetFixtureOrNull(mortar, mortar.Comp.FixtureId) is { } fixture)
            _physics.SetHard(mortar, fixture, true);

        _appearance.SetData(mortar, MortarVisualLayers.State, MortarVisuals.Deployed);

        var xform = Transform(mortar);
        var coordinates = _transform.GetMoverCoordinates(mortar, xform);
        var rotation = Transform(user).LocalRotation.GetCardinalDir().ToAngle();
        _transform.SetCoordinates(mortar, xform, coordinates, rotation);
        _transform.AnchorEntity((mortar, xform));

        if (!_rmcPlanet.IsOnPlanet(coordinates))
            _popup.PopupClient(Loc.GetString("rmc-mortar-deploy-end-not-planet"), user, user, PopupType.MediumCaution);

        _audio.PlayPredicted(mortar.Comp.DeploySound, mortar, user);
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
        if (_rmcPlanet.TryGetOffset(_transform.GetMapCoordinates(mortar.Owner), out var planetOffset))
            offset -= planetOffset;

        mortar.Comp.Target = target;

        var tilesPer = mortar.Comp.TilesPerOffset;
        var xOffset = (int) Math.Floor(Math.Abs(offset.X - position.X) / tilesPer);
        var yOffset = (int) Math.Floor(Math.Abs(offset.Y - position.Y) / tilesPer);
        mortar.Comp.Offset = (_random.Next(-xOffset, xOffset + 1), _random.Next(-yOffset, yOffset + 1));

        Dirty(mortar);
    }

    private void OnMortarDialDoAfter(Entity<MortarComponent> mortar, ref DialMortarDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        mortar.Comp.Dial = args.Vector;
        Dirty(mortar);

        var user = args.User;
        var selfMsg = Loc.GetString("rmc-mortar-dial-finish-self", ("mortar", mortar));
        var othersMsg = Loc.GetString("rmc-mortar-dial-finish-others", ("user", user), ("mortar", mortar));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
    }

    private void OnMortarInteractUsing(Entity<MortarComponent> mortar, ref InteractUsingEvent args)
    {
        var used = args.Used;
        var user = args.User;

        // Check if the used item is a laser designator
        if (TryComp(used, out RangefinderComponent? rangefinder) && rangefinder.CanDesignate)
        {
            // Check if mortar is in laser targeting mode
            if (mortar.Comp.LaserTargetingMode)
            {
                args.Handled = true;
                // If in laser mode, try to link the designator
                TryStartLinkLaserDesignator(mortar, used, user);
                return;
            }
        }

        // Check if the used item is a mortar shell
        if (TryComp(used, out MortarShellComponent? shell))
        {
            args.Handled = true;
            if (!HasSkillPopup(mortar, user, true))
                return;

            if (!CanLoadPopup(mortar, (used, shell), user, out _, out _))
                return;

            var ev = new LoadMortarShellDoAfterEvent();
            var doAfter = new DoAfterArgs(EntityManager, user, shell.LoadDelay, ev, mortar, mortar, used)
            {
                BreakOnMove = true,
                BreakOnHandChange = true,
                ForceVisible = true,
            };

            if (_doAfter.TryStartDoAfter(doAfter))
            {
                var selfMsg = Loc.GetString("rmc-mortar-shell-load-start-self", ("mortar", mortar), ("shell", used));
                var othersMsg = Loc.GetString("rmc-mortar-shell-load-start-others",
                    ("user", user),
                    ("mortar", mortar),
                    ("shell", used));
                _popup.PopupPredicted(selfMsg, othersMsg, mortar, user);

                _audio.PlayPredicted(mortar.Comp.ReloadSound, mortar, user);
            }
        }
    }

    private void OnMortarLoadDoAfter(Entity<MortarComponent> mortar, ref LoadMortarShellDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled || args.Used is not { } shellId)
            return;

        args.Handled = true;
        if (_net.IsClient)
            return;

        if (!TryComp(shellId, out MortarShellComponent? shell))
            return;

        if (!mortar.Comp.Deployed)
            return;

        if (HasComp<ActiveMortarShellComponent>(shellId))
            return;

        if (!CanLoadPopup(mortar, (shellId, shell), user, out var travelTime, out var coordinates))
            return;

        _adminLogs.Add(LogType.RMCMortar, LogImpact.High, $"Mortar {ToPrettyString(mortar)} shell {ToPrettyString(shellId)} shot by {ToPrettyString(user)} aimed at {coordinates}");

        var container = _container.EnsureContainer<Container>(mortar, mortar.Comp.ContainerId);
        if (!_container.Insert(shellId, container))
            return;

        var time = _timing.CurTime;
        mortar.Comp.LastFiredAt = time;

        var active = new ActiveMortarShellComponent
        {
            Coordinates = _transform.ToCoordinates(coordinates),
            WarnAt = time + travelTime,
            ImpactWarnAt = time + travelTime + shell.ImpactWarningDelay,
            LandAt = time + travelTime + shell.ImpactDelay,
        };

        AddComp(shellId, active, true);

        var selfMsg = Loc.GetString("rmc-mortar-shell-load-finish-self", ("mortar", mortar), ("shell", shellId));
        var othersMsg = Loc.GetString("rmc-mortar-shell-load-finish-others", ("user", user), ("mortar", mortar), ("shell", shellId));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);

        othersMsg = Loc.GetString("rmc-mortar-shell-fire", ("mortar", mortar));
        _popup.PopupEntity(othersMsg, mortar, PopupType.MediumCaution);

        var filter = Filter.Pvs(mortar);
        _audio.PlayPvs(mortar.Comp.FireSound, mortar);

        var ev = new MortarFiredEvent(GetNetEntity(mortar));
        if (_net.IsServer)
            RaiseNetworkEvent(ev, filter);
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

        if (_fixture.GetFixtureOrNull(mortar, mortar.Comp.FixtureId) is { } fixture)
            _physics.SetHard(mortar, fixture, false);

        _appearance.SetData(mortar, MortarVisualLayers.State, MortarVisuals.Item);
    }

    private void OnMortarExamined(Entity<MortarComponent> ent, ref ExaminedEvent args)
    {
        if (HasComp<XenoComponent>(args.Examiner))
            return;

        using (args.PushGroup(nameof(MortarComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-mortar-less-accurate-with-range"));
            args.PushMarkup(Loc.GetString(ent.Comp.LaserTargetingMode ? "rmc-mortar-in-laser-mode" : "rmc-mortar-in-coordinates-mode", ("mortar", ent)));

            if (ent.Comp.Deployed && ent.Comp.LaserTargetingMode && ent.Comp.LinkedLaserDesignator != null && ent.Comp.LaserTargetCoordinates != null)
                args.PushMarkup(Loc.GetString("rmc-mortar-laser-aimed", ("mortar", ent)));

            args.PushMarkup(Loc.GetString("rmc-mortar-toggle-mode-hint"));
        }
    }

    private void OnMortarActivatableUIOpenAttempt(Entity<MortarComponent> ent, ref ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!ent.Comp.Deployed)
            args.Cancel();
    }

    private void OnMortarShouldInteract(Entity<MortarComponent> ent, ref CombatModeShouldHandInteractEvent args)
    {
        args.Cancelled = true;
    }

    private void OnMortarCameraShellLand(Entity<MortarCameraShellComponent> ent, ref MortarShellLandEvent args)
    {
        _audio.PlayPvs(ent.Comp.Sound, args.Coordinates);

        var anchored = _rmcMap.GetAnchoredEntitiesEnumerator(args.Coordinates);
        while (anchored.MoveNext(out var uid))
        {
            if (HasComp<MortarCameraComponent>(uid))
                QueueDel(uid);
        }

        var coords = _transform.ToMapCoordinates(args.Coordinates);
        Spawn(ent.Comp.Flare, coords);
        var camera = Spawn(ent.Comp.Camera, coords);

        if (_rmcPlanet.TryGetOffset(coords, out var offset))
            coords = coords.Offset(offset);

        var (x, y) = coords.Position;
        _metaData.SetEntityName(camera, Loc.GetString("rmc-mortar-camera-name", ("x", (int) x), ("y", (int) y)));
    }

    private void OnMortarTargetBui(Entity<MortarComponent> mortar, ref MortarTargetBuiMsg args)
    {
        if (mortar.Comp.LaserTargetingMode)
        {
            _popup.PopupPredictedCursor(Loc.GetString("rmc-mortar-dial-coordinates", ("mortar", mortar)), args.Actor, PopupType.SmallCaution);
            return;
        }

        args.Target.X.Cap(mortar.Comp.MaxTarget);
        args.Target.Y.Cap(mortar.Comp.MaxTarget);

        var user = args.Actor;
        var ev = new TargetMortarDoAfterEvent(args.Target);
        var doAfter = new DoAfterArgs(EntityManager, user, mortar.Comp.TargetDelay, ev, mortar)
        {
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-mortar-target-start-self", ("mortar", mortar));
            var othersMsg = Loc.GetString("rmc-mortar-target-start-others", ("user", user), ("mortar", mortar));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        }
    }

    private void OnMortarDialBui(Entity<MortarComponent> mortar, ref MortarDialBuiMsg args)
    {
        if (mortar.Comp.LaserTargetingMode)
        {
            _popup.PopupPredictedCursor(Loc.GetString("rmc-mortar-dial-coordinates", ("mortar", mortar)), args.Actor, PopupType.SmallCaution);
            return;
        }

        args.Target.X.Cap(mortar.Comp.MaxDial);
        args.Target.Y.Cap(mortar.Comp.MaxDial);

        var user = args.Actor;
        var ev = new DialMortarDoAfterEvent(args.Target);
        var doAfter = new DoAfterArgs(EntityManager, user, mortar.Comp.TargetDelay, ev, mortar)
        {
            BreakOnMove = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var selfMsg = Loc.GetString("rmc-mortar-dial-start-self", ("mortar", mortar));
            var othersMsg = Loc.GetString("rmc-mortar-dial-start-others", ("user", user), ("mortar", mortar));
            _popup.PopupPredicted(selfMsg, othersMsg, user, user);
        }
    }

    private void OnMortarViewCameras(Entity<MortarComponent> ent, ref MortarViewCamerasMsg args)
    {
        _ui.OpenUi(ent.Owner, RMCCameraUiKey.Key, args.Actor);
    }

    private void DeployMortar(Entity<MortarComponent> mortar, EntityUid user)
    {
        if (mortar.Comp.Deployed)
            return;

        if (!CanDeployPopup(mortar, user))
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

    private bool CanDeployPopup(Entity<MortarComponent> mortar, EntityUid user)
    {
        if (!HasSkillPopup(mortar, user, true))
            return false;

        if (!_area.CanMortarPlacement(user.ToCoordinates()))
        {
            _popup.PopupClient(Loc.GetString("rmc-mortar-covered", ("mortar", mortar)), user, user, PopupType.SmallCaution);
            return false;
        }

        return true;
    }

    protected virtual bool CanLoadPopup(
        Entity<MortarComponent> mortar,
        Entity<MortarShellComponent> shell,
        EntityUid user,
        out TimeSpan travelTime,
        out MapCoordinates coordinates)
    {
        travelTime = default;
        coordinates = default;
        return false;
    }

    protected virtual bool ValidateTargetCoordinates(
        Entity<MortarComponent> mortar,
        Entity<MortarShellComponent>? shell,
        MapCoordinates coordinates,
        MapCoordinates mortarCoordinates,
        EntityUid? user,
        out TimeSpan travelTime)
    {
        travelTime = default;
        return false;
    }


    public void PopupWarning(MapCoordinates coordinates, float range, LocId warning, LocId warningAbove, bool chat = false)
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
            _popup.PopupEntity(msg, recipient, recipient, PopupType.LargeCaution);

            if (chat)
            {
                msg = $"[bold][font size=24][color=red]\n{msg}\n[/color][/font][/bold]";
                _rmcChat.ChatMessageToOne(ChatChannel.Radio, msg, msg, default, false, session.Channel);
            }
        }
    }

    private void OnMortarExplosionOnSpawn(Entity<ActivateMortarShellOnSpawnComponent> explosion, ref MapInitEvent args)
    {
        if (!TryComp(explosion, out MortarShellComponent? shell))
            return;

        var coords = _transform.GetMapCoordinates(explosion);
        if (coords.MapId == MapId.Nullspace)
            return;

        var time = _timing.CurTime;

        var active = new ActiveMortarShellComponent
        {
            Coordinates = _transform.ToCoordinates(coords),
            WarnAt = time + shell.TravelDelay,
            ImpactWarnAt = time + shell.TravelDelay + shell.ImpactWarningDelay,
            LandAt = time + shell.TravelDelay + shell.ImpactDelay,
        };

        AddComp(explosion, active, true);
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
                _audio.PlayPvs(active.WarnSound, active.Coordinates);
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

                var ev = new MortarShellLandEvent(active.Coordinates);
                RaiseLocalEvent(uid, ref ev);

                _rmcExplosion.TriggerExplosive(uid);

                if (!EntityManager.IsQueuedForDeletion(uid))
                    QueueDel(uid);
            }
        }
    }

    private void OnMortarLinkLaserDesignatorDoAfter(Entity<MortarComponent> mortar, ref LinkMortarLaserDesignatorDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            mortar.Comp.IsLinking = false;
            Dirty(mortar);
            return;
        }

        args.Handled = true;

        var user = args.User;
        var laserDesignator = GetEntity(args.LaserDesignator);

        mortar.Comp.IsLinking = false;
        mortar.Comp.LinkedLaserDesignator = laserDesignator;
        Dirty(mortar);

        var selfMsg = Loc.GetString("rmc-mortar-laser-linked-self", ("mortar", mortar), ("laserDesignator", laserDesignator));
        var othersMsg = Loc.GetString("rmc-mortar-laser-linked-others", ("user", user), ("mortar", mortar), ("laserDesignator", laserDesignator));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);
    }

    public bool TryToggleLaserTargetingMode(Entity<MortarComponent> mortar, EntityUid user, bool laserMode, bool playSound = true)
    {
        // Xenos cannot toggle mortar targeting mode
        if (HasComp<XenoComponent>(user))
            return false;

        // Mortar must be deployed to toggle targeting mode
        if (!mortar.Comp.Deployed)
        {
            _popup.PopupClient(Loc.GetString("rmc-mortar-not-deployed", ("mortar", mortar)), user, user, PopupType.SmallCaution);
            return false;
        }

        mortar.Comp.LaserTargetingMode = laserMode;

        Dirty(mortar);

        var selfMsg = Loc.GetString(laserMode ? "rmc-mortar-laser-mode-switched-self" : "rmc-mortar-coordinates-mode-switched-self", ("mortar", mortar));
        var othersMsg = Loc.GetString(laserMode ? "rmc-mortar-laser-mode-switched-others" : "rmc-mortar-coordinates-mode-switched-others", ("user", user), ("mortar", mortar));
        _popup.PopupPredicted(selfMsg, othersMsg, user, user);

        if (playSound)
            _audio.PlayPredicted(mortar.Comp.ToggleSound, mortar, user);
        return true;
    }

    public bool TryStartLinkLaserDesignator(Entity<MortarComponent> mortar, EntityUid laserDesignator, EntityUid user)
    {
        if (mortar.Comp.IsLinking)
        {
            _popup.PopupClient(Loc.GetString("rmc-mortar-already-linking"), user, user, PopupType.SmallCaution);
            return false;
        }

        mortar.Comp.IsLinking = true;
        Dirty(mortar);

        var ev = new LinkMortarLaserDesignatorDoAfterEvent(GetNetEntity(laserDesignator));
        var doAfter = new DoAfterArgs(EntityManager, user, mortar.Comp.LaserLinkDelay, ev, mortar)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var msg = Loc.GetString("rmc-mortar-linking-start", ("mortar", mortar), ("laserDesignator", laserDesignator));
            _popup.PopupClient(msg, mortar, user);
            return true;
        }
        else
        {
            mortar.Comp.IsLinking = false;
            Dirty(mortar);
            return false;
        }
    }

    public bool TryUpdateLaserTarget(Entity<MortarComponent> mortar, EntityCoordinates coordinates, bool playSound = true, bool laserTargetDelay = true)
    {
        // Only update if coordinates have actually changed
        if (mortar.Comp.LaserTargetCoordinates == coordinates || coordinates == EntityCoordinates.Invalid)
            return false;

        if (!mortar.Comp.LaserTargetingMode || mortar.Comp.LinkedLaserDesignator == null)
            return false;

        if (laserTargetDelay)
        {
            if (mortar.Comp.IsTargeting)
                return false;

            // Create DoAfter for deferred update. This will also show players a clear progress bar.
            // The mortar literally does something before it's ready to fire,
            // so it'll be clear to the player that this isn't a bug but a delay due to some action.
            // Additionally, this makes it easier to check during this delay.
            EnsureComp<DoAfterComponent>(mortar);

            var ev = new MortarLaserTargetUpdateDoAfterEvent(GetNetCoordinates(coordinates));
            var doAfter = new DoAfterArgs(EntityManager, mortar, mortar.Comp.LaserTargetDelay, ev, mortar)
            {
                NeedHand = false,
                BreakOnMove = true, // The mortar must not move.
                BreakOnHandChange = false,
                BreakOnDamage = false,
                RequireCanInteract = false, // Mortar is not a player
                AttemptFrequency = AttemptFrequency.EveryTick,
            };

            if (_doAfter.TryStartDoAfter(doAfter))
            {
                mortar.Comp.IsTargeting = true;
                Dirty(mortar);
                return true;
            }
        }
        else
        {
            // Update immediately
            mortar.Comp.LaserTargetCoordinates = coordinates;
            mortar.Comp.NeedAnnouncement = true;
            Dirty(mortar);

            if (playSound)
            {
                var sound = mortar.Comp.LaserTargetSound;
                if (mortar.Comp.LaserTargetCoordinates != null)
                {
                    bool validCoordinates = ValidateTargetCoordinates(mortar, null, _transform.ToMapCoordinates(mortar.Comp.LaserTargetCoordinates.Value), _transform.GetMapCoordinates(mortar), null, out _);
                    sound = validCoordinates ? mortar.Comp.LaserTargetSound : mortar.Comp.LaserTargetWarningSound;
                }
                _audio.PlayPvs(sound, mortar);
           }
        }
        return true;
    }

    private void OnMortarLaserTargetUpdateAttempt(Entity<MortarComponent> mortar, ref DoAfterAttemptEvent<MortarLaserTargetUpdateDoAfterEvent> args)
    {
        // Cancel if linked designator lost ActiveLaserDesignatorComponent
        var linked = mortar.Comp.LinkedLaserDesignator;
        if (linked == null || !HasComp<ActiveLaserDesignatorComponent>(linked.Value) || mortar.Comp.Deployed == false)
            args.Cancel();
    }

    private void OnGetMortarVerbs(Entity<MortarComponent> mortar, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess || !args.CanInteract)
            return;

        var user = args.User;

        // Xenos cannot toggle mortar targeting mode
        if (HasComp<XenoComponent>(user))
            return;

        // Toggle targeting mode verb
        var toggleModeVerb = new AlternativeVerb
        {
            Act = () =>
            {
                var laserMode = !mortar.Comp.LaserTargetingMode;
                TryToggleLaserTargetingMode(mortar, user, laserMode);
            },
            Text = Loc.GetString("rmc-mortar-toggle-mode"),
            Message = Loc.GetString("rmc-mortar-toggle-mode-message"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/settings.svg.192dpi.png")),
        };

        args.Verbs.Add(toggleModeVerb);
    }

    private void OnMortarLaserTargetUpdateDoAfter(Entity<MortarComponent> mortar, ref MortarLaserTargetUpdateDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            mortar.Comp.IsTargeting = false;
            mortar.Comp.NeedAnnouncement = true;
            Dirty(mortar);

            _audio.PlayPvs(mortar.Comp.LaserTargetWarningSound, mortar);
            return;
        }

        if (args.Handled)
            return;

        args.Handled = true;

        bool validCoordinates = ValidateTargetCoordinates(mortar, null, _transform.ToMapCoordinates(args.TargetCoordinates), _transform.GetMapCoordinates(mortar), null, out _);
        var sound = validCoordinates ? mortar.Comp.LaserTargetSound : mortar.Comp.LaserTargetWarningSound;

        mortar.Comp.LaserTargetCoordinates = GetCoordinates(args.TargetCoordinates);
        mortar.Comp.IsTargeting = false;
        mortar.Comp.NeedAnnouncement = true;
        Dirty(mortar);

        _audio.PlayPvs(sound, mortar);


    }

    private void OnActiveLaserDesignatorShutdown(Entity<ActiveLaserDesignatorComponent> laserDesignator, ref ComponentShutdown args)
    {
        // Clear targets for all mortars linked to this laser designator
        var mortarQuery = EntityQueryEnumerator<MortarComponent>();
        while (mortarQuery.MoveNext(out var mortarUid, out var mortar))
        {
            if (mortar.LinkedLaserDesignator == laserDesignator.Owner)
            {
                mortar.LaserTargetCoordinates = null;
                mortar.NeedAnnouncement = true; // We notify players that the target has been lost.
                Dirty(mortarUid, mortar);

                _audio.PlayPredicted(mortar.LaserTargetWarningSound, mortarUid, null);
            }
        }
    }

    private void OnRangefinderShutdown(Entity<RangefinderComponent> rangefinder, ref ComponentShutdown args)
    {
        // Clear targets for all mortars linked to this laser designator
        var mortarQuery = EntityQueryEnumerator<MortarComponent>();
        while (mortarQuery.MoveNext(out var mortarUid, out var mortar))
        {
            if (mortar.LinkedLaserDesignator == rangefinder.Owner)
            {
                mortar.LinkedLaserDesignator = null;
                mortar.LaserTargetCoordinates = null;
                mortar.NeedAnnouncement = true; // We notify players that the binding has been forcibly reset.
                Dirty(mortarUid, mortar);

                _audio.PlayPredicted(mortar.LaserTargetWarningSound, mortarUid, null);
            }
        }
    }
}

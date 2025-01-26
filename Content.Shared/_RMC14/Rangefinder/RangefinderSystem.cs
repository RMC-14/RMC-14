using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Dropship.Weapon;
using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Marines.Skills;
using Content.Shared._RMC14.Marines.Squads;
using Content.Shared._RMC14.Rules;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Map;
using Robust.Shared.Network;
using static Content.Shared._RMC14.Rangefinder.RangefinderMode;

namespace Content.Shared._RMC14.Rangefinder;

public sealed class RangefinderSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedDropshipWeaponSystem _dropshipWeapon = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCPlanetSystem _rmcPlanet = default!;
    [Dependency] private readonly SkillsSystem _skills = default!;
    [Dependency] private readonly SquadSystem _squad = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RangefinderComponent, MapInitEvent>(OnRangefinderMapInit);
        SubscribeLocalEvent<RangefinderComponent, AfterInteractEvent>(OnRangefinderAfterInteract);
        SubscribeLocalEvent<RangefinderComponent, LaserDesignatorDoAfterEvent>(OnRangefinderDoAfter);
        SubscribeLocalEvent<RangefinderComponent, ExaminedEvent>(OnRangefinderExamined);
        SubscribeLocalEvent<RangefinderComponent, GetVerbsEvent<AlternativeVerb>>(OnRangefinderGetAlternativeVerbs);

        SubscribeLocalEvent<ActiveLaserDesignatorComponent, ComponentRemove>(OnLaserDesignatorRemove);
        SubscribeLocalEvent<ActiveLaserDesignatorComponent, EntityTerminatingEvent>(OnLaserDesignatorRemove);
        SubscribeLocalEvent<ActiveLaserDesignatorComponent, DroppedEvent>(OnLaserDesignatorDropped);
        SubscribeLocalEvent<ActiveLaserDesignatorComponent, RMCDroppedEvent>(OnLaserDesignatorDropped);
        SubscribeLocalEvent<ActiveLaserDesignatorComponent, GotUnequippedHandEvent>(OnLaserDesignatorDropped);
        SubscribeLocalEvent<ActiveLaserDesignatorComponent, HandDeselectedEvent>(OnLaserDesignatorDropped);

        SubscribeLocalEvent<LaserDesignatorTargetComponent, ComponentRemove>(OnLaserDesignatorTargetRemove);
        SubscribeLocalEvent<LaserDesignatorTargetComponent, EntityTerminatingEvent>(OnLaserDesignatorTargetRemove);
    }

    private void OnRangefinderMapInit(Entity<RangefinderComponent> rangefinder, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        var comp = rangefinder.Comp;
        if (comp.CanDesignate)
            comp.Id = _dropshipWeapon.ComputeNextId();
        else
            comp.Mode = RangefinderMode.Rangefinder;

        if (comp.SwitchModeDelay > TimeSpan.Zero)
        {
            _useDelay.SetLength(rangefinder.Owner,
                comp.SwitchModeDelay,
                comp.SwitchModeUseDelay);
        }

        Dirty(rangefinder);
        UpdateAppearance(rangefinder);
    }

    private void OnRangefinderAfterInteract(Entity<RangefinderComponent> rangefinder, ref AfterInteractEvent args)
    {
        var user = args.User;
        var coordinates = args.ClickLocation.SnapToGrid(EntityManager, _mapManager);
        if (!coordinates.IsValid(EntityManager))
            return;

        args.Handled = true;

        string msg;
        if (!_examine.InRangeUnOccluded(user, coordinates, rangefinder.Comp.Range))
        {
            msg = Loc.GetString("rmc-laser-designator-out-of-range");
            _popup.PopupClient(msg, coordinates, user, PopupType.SmallCaution);
            return;
        }

        var delay = rangefinder.Comp.Delay;
        var skill = _skills.GetSkill(user, rangefinder.Comp.Skill);
        delay -= skill * rangefinder.Comp.TimePerSkillLevel;

        if (delay < rangefinder.Comp.MinimumDelay)
            delay = rangefinder.Comp.MinimumDelay;

        if (rangefinder.Comp.Mode == RangefinderMode.Rangefinder)
        {
            TryTarget(rangefinder, user, delay, coordinates);
            return;
        }

        var grid = _transform.GetGrid(coordinates);
        if (!HasComp<RMCPlanetComponent>(grid))
        {
            msg = Loc.GetString("rmc-laser-designator-not-surface");
            _popup.PopupClient(msg, coordinates, user, PopupType.SmallCaution);
            return;
        }

        if (HasComp<ActiveLaserDesignatorComponent>(rangefinder))
        {
            msg = Loc.GetString("rmc-laser-designator-already-targeting");
            _popup.PopupClient(msg, coordinates, user, PopupType.SmallCaution);
            return;
        }

        if (!_area.CanCAS(coordinates))
        {
            msg = Loc.GetString("rmc-laser-designator-not-cas");
            _popup.PopupClient(msg, coordinates, user, PopupType.SmallCaution);
            return;
        }

        TryTarget(rangefinder, args.User, delay, coordinates);
    }

    private void OnRangefinderDoAfter(Entity<RangefinderComponent> rangefinder, ref LaserDesignatorDoAfterEvent args)
    {
        var user = args.User;
        if (args.Cancelled || args.Handled)
            return;

        args.Handled = true;

        var coords = GetCoordinates(args.Coordinates);
        if (!coords.IsValid(EntityManager))
            return;

        if (rangefinder.Comp.Mode == Designator)
        {
            var msg = Loc.GetString("rmc-laser-designator-acquired");
            _popup.PopupClient(msg, coords, user, PopupType.Medium);
        }

        _audio.PlayPredicted(rangefinder.Comp.AcquireSound, rangefinder, user);

        if (_net.IsClient)
            return;

        var active = EnsureComp<ActiveLaserDesignatorComponent>(rangefinder);
        QueueDel(active.Target);

        var modeLaser = rangefinder.Comp.Mode == Designator
            ? rangefinder.Comp.DesignatorSpawn
            : rangefinder.Comp.RangefinderSpawn;

        coords = _transform.GetMoverCoordinates(coords);
        active.Target = Spawn(modeLaser, coords);
        active.Origin = _transform.GetMoverCoordinates(rangefinder);
        Dirty(rangefinder, active);

        if (rangefinder.Comp.Mode == RangefinderMode.Rangefinder)
        {
            var mapCoords = _transform.ToMapCoordinates(coords);
            var position = mapCoords.Position.Floored();
            if (_rmcPlanet.TryGetOffset(mapCoords, out var offset))
                position += offset;

            rangefinder.Comp.LastTarget = position;
            Dirty(rangefinder);

            _ui.OpenUi(rangefinder.Owner, RangefinderUiKey.Key, args.User);
            return;
        }

        var targetEnt = active.Target.Value;
        var target = EnsureComp<LaserDesignatorTargetComponent>(targetEnt);
        var id = EnsureId(rangefinder);
        target.Id = id;
        Dirty(targetEnt, target);

        var name = Loc.GetString("rmc-laser-designator-target-name", ("id", id));
        var abbreviation = _dropshipWeapon.GetUserAbbreviation(user, id);
        if (_squad.TryGetMemberSquad(user, out var squad))
            name = Loc.GetString("rmc-laser-designator-target-name-squad", ("squad", squad), ("id", id));

        _dropshipWeapon.MakeDropshipTarget(targetEnt, abbreviation);
        _metaData.SetEntityName(targetEnt, name);
    }

    private void OnRangefinderExamined(Entity<RangefinderComponent> rangefinder, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(RangefinderComponent)))
        {
            var comp = rangefinder.Comp;
            if (comp.LastTarget is { } target)
                args.PushMarkup(Loc.GetString("rmc-rangefinder-examine", ("item", rangefinder), ("x", target.X), ("y", target.Y)));

            if (comp.Id is { } id)
                args.PushMarkup(Loc.GetString("rmc-laser-designator-examine-id", ("id", id)));

            if (comp.CanDesignate)
            {
                switch (comp.Mode)
                {
                    case RangefinderMode.Rangefinder:
                    {
                        var msg = Loc.GetString("rmc-laser-designator-in-rangefinder-mode", ("item", rangefinder));
                        args.PushMarkup(msg);
                        break;
                    }
                    case Designator:
                    {
                        var msg = Loc.GetString("rmc-laser-designator-in-designator-mode", ("item", rangefinder));
                        args.PushMarkup(msg);
                        break;
                    }
                }

                args.PushMarkup(Loc.GetString("rmc-laser-designator-to-switch"));
            }
        }
    }

    private void OnRangefinderGetAlternativeVerbs(Entity<RangefinderComponent> rangefinder, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        var nextMode = rangefinder.Comp.Mode == RangefinderMode.Rangefinder ? Designator : RangefinderMode.Rangefinder;
        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 100,
            Act = () => ChangeDesignatorMode(rangefinder, nextMode),
            Text = Loc.GetString("rmc-laser-designator-switch-mode", ("mode", nextMode)),
        });
    }

    private void OnLaserDesignatorRemove<T>(Entity<ActiveLaserDesignatorComponent> active, ref T args)
    {
        if (_net.IsClient)
            return;

        Del(active.Comp.Target);
    }

    private void OnLaserDesignatorDropped<T>(Entity<ActiveLaserDesignatorComponent> active, ref T args)
    {
        RemCompDeferred<ActiveLaserDesignatorComponent>(active);
    }

    private void OnLaserDesignatorTargetRemove<T>(Entity<LaserDesignatorTargetComponent> target, ref T args)
    {
        if (TryComp(target.Comp.LaserDesignator, out ActiveLaserDesignatorComponent? active))
        {
            active.Target = null;
            Dirty(target.Comp.LaserDesignator.Value, active);
        }
    }

    private void ChangeDesignatorMode(Entity<RangefinderComponent> rangefinder, RangefinderMode mode)
    {
        if (!rangefinder.Comp.CanDesignate)
            return;

        var delay = rangefinder.Comp.SwitchModeUseDelay;
        if (TryComp(rangefinder, out UseDelayComponent? useDelay))
        {
            if (_useDelay.IsDelayed((rangefinder, useDelay), delay))
                return;

            _useDelay.TryResetDelay(rangefinder, component: useDelay, id: delay);
        }

        if (rangefinder.Comp.DoAfter != null && _doAfter.IsRunning(rangefinder.Comp.DoAfter.Id))
            _doAfter.Cancel(rangefinder.Comp.DoAfter.Id);

        rangefinder.Comp.Mode = mode;
        Dirty(rangefinder);
        UpdateAppearance(rangefinder);
    }

    private void UpdateAppearance(Entity<RangefinderComponent> rangefinder)
    {
        _appearance.SetData(rangefinder, RangefinderLayers.Layer, rangefinder.Comp.Mode);
    }

    private int EnsureId(Entity<RangefinderComponent> rangefinder)
    {
        rangefinder.Comp.Id ??= _dropshipWeapon.ComputeNextId();
        return rangefinder.Comp.Id.Value;
    }

    private void TryTarget(Entity<RangefinderComponent> rangefinder, EntityUid user, TimeSpan delay, EntityCoordinates coordinates)
    {
        if (TryComp(rangefinder, out UseDelayComponent? useDelay))
        {
            if (_useDelay.IsDelayed((rangefinder, useDelay)))
                return;

            _useDelay.TryResetDelay(rangefinder, component: useDelay);
        }

        var ev = new LaserDesignatorDoAfterEvent(GetNetCoordinates(coordinates));
        var doAfter = new DoAfterArgs(EntityManager, user, delay, ev, rangefinder)
        {
            BreakOnMove = true,
            NeedHand = true,
            BreakOnHandChange = true,
        };

        if (_doAfter.TryStartDoAfter(doAfter))
        {
            var msg = Loc.GetString("rmc-laser-designator-start");
            _popup.PopupClient(msg, coordinates, user, PopupType.Medium);
            _audio.PlayPredicted(rangefinder.Comp.TargetSound, rangefinder, user);

            rangefinder.Comp.DoAfter = ev.DoAfter;
        }
    }
}

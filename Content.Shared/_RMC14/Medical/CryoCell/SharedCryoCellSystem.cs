using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Storage;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stunnable;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Medical.CryoCell;

public abstract class SharedCryoCellSystem : EntitySystem
{
    [Dependency] private readonly AreaSystem _area = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedPointLightSystem _light = default!;
    [Dependency] private readonly SharedMarineAnnounceSystem _marineAnnounce = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMovementSystem _rmcMovement = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, ComponentInit>(OnCryoCellInit);
        SubscribeLocalEvent<CryoCellComponent, PowerChangedEvent>(OnCryoCellPower);
        SubscribeLocalEvent<CryoCellComponent, EntInsertedIntoContainerMessage>(OnCryoCellEntInserted);
        SubscribeLocalEvent<CryoCellComponent, EntRemovedFromContainerMessage>(OnCryoCellEntRemoved);

        SubscribeLocalEvent<InsideCryoCellComponent, MoveInputEvent>(OnInsideCryoCellMoveInput);
    }

    private void OnCryoCellInit(Entity<CryoCellComponent> cryoCell, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(cryoCell, cryoCell.Comp.OccupantId);
        UpdateCryoCellVisuals(cryoCell);
    }

    private void OnCryoCellPower(Entity<CryoCellComponent> cryoCell, ref PowerChangedEvent args)
    {
        if (!args.Powered)
            _ui.CloseUi(cryoCell.Owner, CryoCellUIKey.Key);

        UpdateCryoCellVisuals(cryoCell, args.Powered);
    }

    private void OnCryoCellEntInserted(Entity<CryoCellComponent> cryoCell, ref EntInsertedIntoContainerMessage args)
    {
        if (args.Container.ID != cryoCell.Comp.OccupantId)
            return;

        cryoCell.Comp.Occupant = args.Entity;

        Dirty(cryoCell);
        UpdateCryoCellVisuals(cryoCell);

        if (!_timing.ApplyingState)
            EnsureComp<InsideCryoCellComponent>(args.Entity).Chamber = cryoCell;
    }

    private void OnCryoCellEntRemoved(Entity<CryoCellComponent> cryoCell, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != cryoCell.Comp.OccupantId)
            return;

        if (cryoCell.Comp.Occupant == args.Entity)
        {
            cryoCell.Comp.Occupant = null;
            Dirty(cryoCell);
        }

        UpdateCryoCellVisuals(cryoCell);
        RemCompDeferred<InsideCryoCellComponent>(args.Entity);
        _rmcMovement.SuppressCollisionOnExit(args.Entity, cryoCell.Owner);
    }

    private void OnInsideCryoCellMoveInput(Entity<InsideCryoCellComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Chamber is not { } cellId)
            return;

        if (!TryComp<CryoCellComponent>(cellId, out var cellComp))
            return;

        EjectOccupant((cellId, cellComp), ent);
    }

    protected void EjectOccupant(Entity<CryoCellComponent> cryoCell, EntityUid occupant, bool dead = false, bool isAutoEject = false)
    {
        if (!_container.TryGetContainer(cryoCell, cryoCell.Comp.OccupantId, out var container))
            return;

        _container.Remove(occupant, container);

        if (cryoCell.Comp.ExitStun > TimeSpan.Zero && HasComp<NoStunOnExitComponent>(cryoCell))
            _stun.TryStun(occupant, cryoCell.Comp.ExitStun, true);

        _audio.PlayPvs(cryoCell.Comp.EjectSound, cryoCell);

        if (isAutoEject)
        {
            cryoCell.Comp.IsPoweredOn = false;
            if (cryoCell.Comp.ReleaseNotice)
            {
                var areaName = _area.GetAreaName(cryoCell);
                var reason = dead
                    ? Loc.GetString("rmc-cryo-cell-auto-eject-reason-dead")
                    : Loc.GetString("rmc-cryo-cell-auto-eject-reason-recovery");

                var announce = Loc.GetString("rmc-cryo-cell-auto-eject-reason-release",
                    ("occupant", occupant),
                    ("cryoCell", cryoCell.Owner),
                    ("area", areaName),
                    ("reason", reason));
                _marineAnnounce.AnnounceRadio(cryoCell, announce, cryoCell.Comp.ReleaseNoticeRadioChannel);
            }
        }

        Dirty(cryoCell);
        UpdateCryoCellVisuals(cryoCell);
    }

    protected void CryoPopupAndSound(Entity<CryoCellComponent> cryoCell, string msg, bool silent = false, bool warningSound = false)
    {
        if (!silent)
        {
            if (warningSound)
            {
                _audio.PlayPvs(cryoCell.Comp.BeepBeep, cryoCell);
                _popup.PopupEntity(Loc.GetString("rmc-cryo-cell-popup-beep", ("cryoCell", cryoCell.Owner), ("msg", msg)), cryoCell, PopupType.MediumCaution);
            }
            else
            {
                _audio.PlayPvs(cryoCell.Comp.Ping, cryoCell);
                _popup.PopupEntity(Loc.GetString("rmc-cryo-cell-popup-ping", ("cryoCell", cryoCell.Owner), ("msg", msg)), cryoCell, PopupType.Medium);
            }
        }
    }

    protected void UpdateCryoCellVisuals(Entity<CryoCellComponent> cryoCell, bool? powered = null)
    {
        var isOn = cryoCell.Comp.IsPoweredOn && (powered ?? true);
        var hasOccupant = cryoCell.Comp.Occupant != null;

        if (_light.TryGetLight(cryoCell.Owner, out var light))
            _light.SetEnabled(cryoCell.Owner, isOn && hasOccupant, light);

        var newState = (isOn, hasOccupant) switch
        {
            (true, false) => CryoCellVisualState.OnEmpty,
            (true, true) => CryoCellVisualState.OnOccupied,
            (false, false) => CryoCellVisualState.OffEmpty,
            (false, true) => CryoCellVisualState.OffOccupied,
        };

        if (_appearance.TryGetData<CryoCellVisualState>(cryoCell.Owner, CryoCellVisuals.State, out var oldState) &&
            oldState == newState)
        {
            return;
        }

        _appearance.SetData(cryoCell, CryoCellVisuals.State, newState);
    }
}

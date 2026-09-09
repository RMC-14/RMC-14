using Content.Shared._RMC14.Areas;
using Content.Shared._RMC14.Marines.Announce;
using Content.Shared._RMC14.Movement;
using Content.Shared._RMC14.Storage;
using Content.Shared.Mobs.Systems;
using Content.Shared.Movement.Events;
using Content.Shared.Popups;
using Content.Shared.Power;
using Content.Shared.Stunnable;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Network;
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
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly RMCMovementSystem _rmcMovement = default!;
    [Dependency] private readonly SharedStunSystem _stun = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedUserInterfaceSystem _ui = default!;
    [Dependency] private readonly SharedVerbSystem _verb = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CryoCellComponent, ComponentInit>(OnCryoCellInit);
        SubscribeLocalEvent<CryoCellComponent, PowerChangedEvent>(OnCryoCellPower);
        SubscribeLocalEvent<CryoCellComponent, EntInsertedIntoContainerMessage>(OnCryoCellEntInserted);
        SubscribeLocalEvent<CryoCellComponent, EntRemovedFromContainerMessage>(OnCryoCellEntRemoved);
        SubscribeLocalEvent<CryoCellComponent, GetVerbsEvent<AlternativeVerb>>(OnGetAltVerbs);

        SubscribeLocalEvent<InsideCryoCellComponent, MoveInputEvent>(OnInsideCryoCellMoveInput);
    }

    private void OnCryoCellInit(Entity<CryoCellComponent> cryoCell, ref ComponentInit args)
    {
        _container.EnsureContainer<ContainerSlot>(cryoCell, cryoCell.Comp.OccupantSlot);
        _container.EnsureContainer<ContainerSlot>(cryoCell, cryoCell.Comp.BeakerSlot);
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
        if (args.Container.ID != cryoCell.Comp.OccupantSlot)
            return;

        cryoCell.Comp.Occupant = args.Entity;

        Dirty(cryoCell);
        UpdateCryoCellVisuals(cryoCell);

        if (!_timing.ApplyingState)
            EnsureComp<InsideCryoCellComponent>(args.Entity).Chamber = cryoCell;
    }

    private void OnCryoCellEntRemoved(Entity<CryoCellComponent> cryoCell, ref EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != cryoCell.Comp.OccupantSlot)
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

    private void OnGetAltVerbs(Entity<CryoCellComponent> cryoCell, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanInteract || !args.CanAccess)
            return;

        if (cryoCell.Comp.Occupant is not { } occupant)
            return;

        if (args.User == occupant)
        {
            if (_mobState.IsIncapacitated(occupant))
                return;

            args.Verbs.Add(new AlternativeVerb
            {
                Text = Loc.GetString("rmc-cryo-cell-verb-eject-inside"),
                ConfirmationPopup = true,
                Category = VerbCategory.Eject,
                Act = () => StartDelayedEject(cryoCell, occupant),
            });

            return;
        }

        // Outside user gets normal eject
        args.Verbs.Add(new AlternativeVerb
        {
            Text = Loc.GetString("rmc-cryo-cell-verb-eject-outside"),
            Category = VerbCategory.Eject,
            Act = () => EjectOccupant(cryoCell, occupant),
        });
    }

    private void OnInsideCryoCellMoveInput(Entity<InsideCryoCellComponent> ent, ref MoveInputEvent args)
    {
        if (!args.HasDirectionalMovement)
            return;

        if (_timing.ApplyingState)
            return;

        if (ent.Comp.Chamber is not { } cellId)
            return;

        if (_mobState.IsIncapacitated(ent))
            return;

        foreach (var verb in _verb.GetLocalVerbs(cellId, ent.Owner, typeof(Verb)))
        {
            if (!verb.Text.Equals(Loc.GetString("rmc-cryo-cell-verb-eject-inside")))
                continue;

            _verb.ExecuteVerb(verb, ent.Owner, cellId);
            break;
        }
    }

    private void StartDelayedEject(Entity<CryoCellComponent> cryoCell, EntityUid occupant)
    {
        if (cryoCell.Comp.Occupant != occupant)
            return;

        Timer.Spawn(TimeSpan.FromSeconds(30), () => FinishDelayedEject(cryoCell, occupant));
    }

    private void FinishDelayedEject(Entity<CryoCellComponent> cryoCell, EntityUid occupant)
    {
        if (TerminatingOrDeleted(cryoCell))
            return;

        if (TerminatingOrDeleted(occupant))
            return;

        if (cryoCell.Comp.Occupant != occupant)
            return;

        EjectOccupant(cryoCell, occupant);
    }

    protected void EjectOccupant(Entity<CryoCellComponent> cryoCell, EntityUid occupant, bool dead = false, bool isAutoEject = false)
    {
        if (!_container.TryGetContainer(cryoCell, cryoCell.Comp.OccupantSlot, out var container))
            return;

        _container.Remove(occupant, container);

        if (cryoCell.Comp.ExitStun > TimeSpan.Zero && HasComp<NoStunOnExitComponent>(cryoCell))
            _stun.TryStun(occupant, cryoCell.Comp.ExitStun, true);

        if (_net.IsServer)
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
                    ("occupant", Name(occupant)),
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
        if (_net.IsClient)
            return;

        if (silent)
            return;

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

    protected void UpdateCryoCellVisuals(Entity<CryoCellComponent> cryoCell, bool? powered = null)
    {
        if (!TryComp<AppearanceComponent>(cryoCell, out var appearance))
            return;

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

        if (_appearance.TryGetData<CryoCellVisualState>(cryoCell.Owner, CryoCellVisuals.State, out var oldState, appearance) &&
            oldState == newState)
        {
            return;
        }

        _appearance.SetData(cryoCell, CryoCellVisuals.State, newState);
    }
}

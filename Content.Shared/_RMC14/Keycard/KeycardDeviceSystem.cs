using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Dialog;
using Content.Shared._RMC14.OrbitalCannon;
using Content.Shared._RMC14.Power;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Administration.Logs;
using Content.Shared.Coordinates;
using Content.Shared.Database;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Keycard;

public sealed class KeycardDeviceSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly RMCAlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly OrbitalCannonSystem _orbitalCannon = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedRMCPowerSystem _power = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly ProtoId<AccessLevelPrototype> SeniorCommand = "RMCAccessSeniorCommand";

    private readonly HashSet<Entity<KeycardDeviceComponent>> _devices = new();
    private readonly HashSet<EntityUid> _pendingSources = new();
    private readonly HashSet<EntityUid> _toRemove = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<KeycardDeviceComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<KeycardDeviceComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<KeycardDeviceComponent, KeycardDeviceSetModeEvent>(OnSetMode);
        SubscribeLocalEvent<KeycardDeviceComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnShutdown(Entity<KeycardDeviceComponent> ent, ref ComponentShutdown args)
    {
        if (_net.IsClient || !_pendingSources.Remove(ent))
            return;

        var query = EntityQueryEnumerator<KeycardDeviceComponent>();
        while (query.MoveNext(out var uid, out var device))
        {
            if (device.RequestSource == ent.Owner)
                ResetDevice((uid, device));
        }
    }

    private void OnInteractHand(Entity<KeycardDeviceComponent> ent, ref InteractHandEvent args)
    {
        if (!_power.IsPowered(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-keycard-unpowered"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (!_accessReader.IsAllowed(args.User, ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-access-denied"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Active)
        {
            _popup.PopupClient(Loc.GetString("rmc-keycard-busy"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-alert-red-alert"), new KeycardDeviceSetModeEvent(KeycardDeviceMode.RedAlert)),
            new(Loc.GetString("rmc-keycard-ob-safety"), new KeycardDeviceSetModeEvent(KeycardDeviceMode.OrbitalBombardmentSafety)),
        };
        _dialog.OpenOptions(
            ent,
            args.User,
            Loc.GetString("rmc-keycard-device"),
            options,
            Loc.GetString("rmc-keycard-device-description"));
    }

    private void OnSetMode(Entity<KeycardDeviceComponent> ent, ref KeycardDeviceSetModeEvent args)
    {
        if (_net.IsClient || ent.Comp.Active)
            return;

        ent.Comp.Mode = args.Mode;
        Dirty(ent);
    }

    private void OnInteractUsing(Entity<KeycardDeviceComponent> ent, ref InteractUsingEvent args)
    {
        if (_net.IsClient)
            return;

        if (!_power.IsPowered(ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-keycard-unpowered"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (!TryComp(ent, out AccessReaderComponent? accessReader))
            return;

        var access = _accessReader.FindAccessTags(args.Used);
        if (!_accessReader.AreAccessTagsAllowed(access, accessReader))
        {
            _popup.PopupClient(Loc.GetString("rmc-access-denied"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;
        if (ent.Comp.RequestSource is { } source)
        {
            ConfirmRequest(ent, source, args.User);
            return;
        }

        if (ent.Comp.Active)
        {
            _popup.PopupClient(Loc.GetString("rmc-keycard-busy"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Mode == KeycardDeviceMode.None)
        {
            _popup.PopupClient(Loc.GetString("rmc-keycard-select-event"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        if (ent.Comp.Mode == KeycardDeviceMode.OrbitalBombardmentSafety && !access.Contains(SeniorCommand))
        {
            _popup.PopupClient(Loc.GetString("rmc-keycard-senior-command-required"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        BeginRequest(ent, args.User);
    }

    private void BeginRequest(Entity<KeycardDeviceComponent> source, EntityUid user)
    {
        var expiresAt = _timing.CurTime + source.Comp.Time;
        source.Comp.Active = true;
        source.Comp.Initiator = user;
        source.Comp.RequestExpiresAt = expiresAt;
        SetAppearance(source);
        Dirty(source);

        _devices.Clear();
        _entityLookup.GetEntitiesInRange(source.Owner.ToCoordinates(), source.Comp.Range, _devices);
        var receivers = 0;
        foreach (var receiver in _devices)
        {
            if (receiver.Owner == source.Owner || receiver.Comp.Active || !_power.IsPowered(receiver))
                continue;

            receiver.Comp.Active = true;
            receiver.Comp.Mode = source.Comp.Mode;
            receiver.Comp.RequestSource = source;
            receiver.Comp.RequestExpiresAt = expiresAt;
            SetAppearance(receiver);
            Dirty(receiver);
            receivers++;
        }

        if (receivers == 0)
        {
            ResetDevice(source);
            _popup.PopupClient(Loc.GetString("rmc-keycard-no-second-device"), source, user, PopupType.SmallCaution);
            return;
        }

        _pendingSources.Add(source);
        _popup.PopupClient(Loc.GetString("rmc-keycard-awaiting-confirmation"), source, user, PopupType.Small);
    }

    private void ConfirmRequest(
        Entity<KeycardDeviceComponent> receiver,
        EntityUid sourceId,
        EntityUid confirmer)
    {
        if (receiver.Owner == sourceId)
        {
            _popup.PopupClient(Loc.GetString("rmc-keycard-distinct-device-required"), receiver, confirmer, PopupType.SmallCaution);
            return;
        }

        if (!TryComp(sourceId, out KeycardDeviceComponent? source) ||
            !source.Active ||
            source.RequestExpiresAt < _timing.CurTime)
        {
            ResetRequest(sourceId);
            _popup.PopupClient(Loc.GetString("rmc-keycard-request-expired"), receiver, confirmer, PopupType.SmallCaution);
            return;
        }

        if (source.Initiator is not { } initiator)
        {
            ResetRequest(sourceId);
            return;
        }

        switch (source.Mode)
        {
            case KeycardDeviceMode.RedAlert:
                _alertLevel.Set(RMCAlertLevels.Red, initiator);
                _adminLog.Add(
                    LogType.RMCAlertLevel,
                    $"{ToPrettyString(initiator):player} initiated and {ToPrettyString(confirmer):player} confirmed Red Alert keycard authentication");
                _popup.PopupClient(Loc.GetString("rmc-keycard-red-alert-confirmed"), receiver, confirmer, PopupType.Small);
                break;
            case KeycardDeviceMode.OrbitalBombardmentSafety:
                var engaged = _orbitalCannon.ToggleSafety(initiator, confirmer);
                var message = engaged
                    ? Loc.GetString("rmc-keycard-ob-safety-engaged")
                    : Loc.GetString("rmc-keycard-ob-safety-disengaged");
                _popup.PopupClient(message, receiver, confirmer, PopupType.Small);
                break;
            case KeycardDeviceMode.None:
            default:
                Log.Warning($"Unknown {nameof(KeycardDeviceMode)}: {source.Mode}");
                break;
        }

        ResetRequest(sourceId);
    }

    private void ResetRequest(EntityUid source)
    {
        _pendingSources.Remove(source);
        _devices.Clear();

        if (TryComp(source, out KeycardDeviceComponent? sourceComp))
            _entityLookup.GetEntitiesInRange(source.ToCoordinates(), sourceComp.Range, _devices);

        foreach (var device in _devices)
        {
            if (device.Owner == source || device.Comp.RequestSource == source)
                ResetDevice(device);
        }
    }

    private void ResetDevice(Entity<KeycardDeviceComponent> device)
    {
        device.Comp.Active = false;
        device.Comp.Mode = KeycardDeviceMode.None;
        device.Comp.RequestSource = null;
        device.Comp.Initiator = null;
        device.Comp.RequestExpiresAt = default;
        SetAppearance(device);
        Dirty(device);
    }

    private void SetAppearance(Entity<KeycardDeviceComponent> device)
    {
        _appearance.SetData(device, KeycardDeviceVisuals.Active, device.Comp.Active);
    }

    public override void Update(float frameTime)
    {
        if (_net.IsClient || _pendingSources.Count == 0)
            return;

        _toRemove.Clear();
        foreach (var source in _pendingSources)
        {
            if (!TryComp(source, out KeycardDeviceComponent? device) ||
                device.RequestExpiresAt <= _timing.CurTime)
            {
                _toRemove.Add(source);
            }
        }

        foreach (var source in _toRemove)
            ResetRequest(source);
    }
}

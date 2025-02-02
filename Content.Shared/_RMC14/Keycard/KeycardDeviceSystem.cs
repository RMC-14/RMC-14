using Content.Shared._RMC14.AlertLevel;
using Content.Shared._RMC14.Dialog;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Coordinates;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Shared._RMC14.Keycard;

public sealed class KeycardDeviceSystem : EntitySystem
{
    [Dependency] private readonly AccessReaderSystem _accessReader = default!;
    [Dependency] private readonly RMCAlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly DialogSystem _dialog = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<Entity<KeycardDeviceComponent>> _devices = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<KeycardDeviceComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<KeycardDeviceComponent, KeycardDeviceSetModeEvent>(OnSetMode);
        SubscribeLocalEvent<KeycardDeviceComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractHand(Entity<KeycardDeviceComponent> ent, ref InteractHandEvent args)
    {
        if (!_accessReader.IsAllowed(args.User, ent))
        {
            _popup.PopupClient(Loc.GetString("rmc-access-denied"), ent, args.User, PopupType.SmallCaution);
            return;
        }

        // TODO RMC14 ERT, enable/disable maintenance security
        var options = new List<DialogOption>
        {
            new(Loc.GetString("rmc-alert-red-alert"), new KeycardDeviceSetModeEvent(KeycardDeviceMode.RedAlert)),
        };
        _dialog.OpenOptions(ent,
            args.User,
            Loc.GetString("rmc-keycard-device"),
            options,
            Loc.GetString("rmc-keycard-device-description")
        );
    }

    private void OnSetMode(Entity<KeycardDeviceComponent> ent, ref KeycardDeviceSetModeEvent args)
    {
        ent.Comp.Mode = args.Mode;
        Dirty(ent);
    }

    private void OnInteractUsing(Entity<KeycardDeviceComponent> ent, ref InteractUsingEvent args)
    {
        if (TryComp(ent, out AccessReaderComponent? accessReader))
        {
            var access = _accessReader.FindAccessTags(args.Used);
            if (!_accessReader.AreAccessTagsAllowed(access, accessReader))
            {
                _popup.PopupClient(Loc.GetString("rmc-access-denied"), ent, args.User, PopupType.SmallCaution);
                return;
            }
        }

        var time = _timing.CurTime;
        ent.Comp.LastActivated = time;
        Dirty(ent);

        if (!AllEnabled(ent))
            return;

        switch (ent.Comp.Mode)
        {
            case KeycardDeviceMode.None:
                return;
            case KeycardDeviceMode.RedAlert:
                _alertLevel.Set(RMCAlertLevels.Red, args.User);
                break;
            default:
                Log.Warning($"Unknown {nameof(KeycardDeviceMode)}: {ent.Comp.Mode}");
                return;
        }
    }

    private bool AllEnabled(Entity<KeycardDeviceComponent> ent)
    {
        _devices.Clear();
        _entityLookup.GetEntitiesInRange(ent.Owner.ToCoordinates(), ent.Comp.Range, _devices);

        var time = _timing.CurTime;
        foreach (var device in _devices)
        {
            if (ent.Comp.Mode != device.Comp.Mode)
                return false;

            if (device.Comp.LastActivated < time - device.Comp.Time)
                return false;
        }

        return true;
    }
}

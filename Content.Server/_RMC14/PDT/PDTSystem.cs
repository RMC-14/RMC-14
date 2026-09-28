using System.Numerics;
using Content.Server.PowerCell;
using Content.Shared._RMC14.Maths;
using Content.Shared._RMC14.PDT;
using Content.Shared._RMC14.Storage;
using Content.Shared._RMC14.UniformAccessories;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._RMC14.PDT;

public sealed class PDTSystem : EntitySystem
{
    private const float LowBatteryFraction = 0.5f;

    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly PowerCellSystem _cell = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    private EntityQuery<PDTBraceletComponent> _braceletQuery;
    private EntityQuery<PDTLocatorComponent> _locatorQuery;

    public override void Initialize()
    {
        _braceletQuery = GetEntityQuery<PDTBraceletComponent>();
        _locatorQuery = GetEntityQuery<PDTLocatorComponent>();

        SubscribeLocalEvent<PDTKitComponent, CMStorageItemFillEvent>(OnKitStorageFill);
        SubscribeLocalEvent<PDTLocatorComponent, MapInitEvent>(OnLocatorMapInit);
        SubscribeLocalEvent<PDTLocatorComponent, PowerCellChangedEvent>(OnLocatorPowerCellChanged);
        SubscribeLocalEvent<PDTLocatorComponent, PowerCellSlotEmptyEvent>(OnLocatorPowerCellEmpty);
        SubscribeLocalEvent<PDTLocatorComponent, UseInHandEvent>(OnLocatorUseInHand);
        SubscribeLocalEvent<PDTLocatorComponent, InteractUsingEvent>(OnLocatorInteractUsing);
        SubscribeLocalEvent<PDTBraceletComponent, InteractUsingEvent>(OnBraceletInteractUsing);
        SubscribeLocalEvent<PDTBraceletHolderTargetComponent, InteractUsingEvent>(OnAccessoryHolderInteractUsing);
        SubscribeLocalEvent<PDTLocatorComponent, ExaminedEvent>(OnLocatorExamined);
        SubscribeLocalEvent<PDTBraceletComponent, ExaminedEvent>(OnBraceletExamined);
    }

    private void OnKitStorageFill(Entity<PDTKitComponent> ent, ref CMStorageItemFillEvent args)
    {
        if (_locatorQuery.HasComp(args.Item))
            ent.Comp.Locator = args.Item;
        else if (_braceletQuery.HasComp(args.Item))
            ent.Comp.Bracelet = args.Item;
        else
            return;

        if (ent.Comp.Locator is not { } locator ||
            ent.Comp.Bracelet is not { } bracelet ||
            !_locatorQuery.TryComp(locator, out var locatorComp) ||
            !_braceletQuery.TryComp(bracelet, out var braceletComp))
        {
            return;
        }

        Link((locator, locatorComp), (bracelet, braceletComp));
    }

    private void OnLocatorMapInit(Entity<PDTLocatorComponent> ent, ref MapInitEvent args)
    {
        UpdateLocatorVisuals(ent);
    }

    private void OnLocatorPowerCellChanged(Entity<PDTLocatorComponent> ent, ref PowerCellChangedEvent args)
    {
        UpdateLocatorVisuals(ent);
    }

    private void OnLocatorPowerCellEmpty(Entity<PDTLocatorComponent> ent, ref PowerCellSlotEmptyEvent args)
    {
        UpdateLocatorVisuals(ent);
    }

    private void OnLocatorUseInHand(Entity<PDTLocatorComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;

        if (TryComp(ent, out UseDelayComponent? useDelay) &&
            _useDelay.IsDelayed((ent.Owner, useDelay)))
        {
            return;
        }

        if (!TryGetSignal(ent, out var bracelet, out var locatorCoords, out var braceletCoords, args.User))
            return;

        if (!_cell.TryUseCharge(ent, ent.Comp.PingCharge, user: args.User))
        {
            UpdateLocatorVisuals(ent);
            return;
        }

        if (useDelay != null)
            _useDelay.TryResetDelay((ent.Owner, useDelay));

        UpdateLocatorVisuals(ent);

        var distance = Vector2.Distance(locatorCoords.Position, braceletCoords.Position);
        var roundedDistance = (int)MathF.Round(distance);
        var readout = roundedDistance <= 1
            ? Loc.GetString("rmc-pdt-locator-scan-close-readout")
            : Loc.GetString("rmc-pdt-locator-scan-readout",
                ("distance", roundedDistance),
                ("direction", GetDirection(braceletCoords.Position - locatorCoords.Position)));

        var scanner = args.User;
        _popup.PopupEntity(Loc.GetString("rmc-pdt-locator-scan-result", ("readout", readout)), ent, scanner, PopupType.Medium);

        var braceletFilter = Filter.Pvs(bracelet.Owner, entityManager: EntityManager)
            .RemovePlayerByAttachedEntity(scanner);
        _popup.PopupEntity(Loc.GetString("rmc-pdt-bracelet-ping", ("bracelet", bracelet.Owner)), bracelet.Owner, braceletFilter, true);

        _audio.PlayPvs(ent.Comp.PingSound, ent);
        _audio.PlayPvs(bracelet.Comp.PingSound, bracelet.Owner);
    }

    private void OnLocatorInteractUsing(Entity<PDTLocatorComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_braceletQuery.TryComp(args.Used, out var bracelet))
            return;

        args.Handled = true;
        TryPairWithDelay(ent, (args.Used, bracelet), args.User);
    }

    private void OnBraceletInteractUsing(Entity<PDTBraceletComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_locatorQuery.TryComp(args.Used, out var locator))
            return;

        args.Handled = true;
        TryPairWithDelay((args.Used, locator), ent, args.User);
    }

    private void OnAccessoryHolderInteractUsing(Entity<PDTBraceletHolderTargetComponent> ent, ref InteractUsingEvent args)
    {
        if (args.Handled || !_locatorQuery.TryComp(args.Used, out var locator))
            return;

        if (!TryComp<UniformAccessoryHolderComponent>(ent, out var holder) ||
            !_container.TryGetContainer(ent.Owner, holder.ContainerId, out var container))
        {
            return;
        }

        foreach (var accessory in container.ContainedEntities)
        {
            if (!_braceletQuery.TryComp(accessory, out var bracelet))
                continue;

            args.Handled = true;
            TryPairWithDelay((args.Used, locator), (accessory, bracelet), args.User);
            return;
        }
    }

    private void OnLocatorExamined(Entity<PDTLocatorComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(PDTLocatorComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-pdt-examine-serial", ("serial", GetSerial(ent.Comp))));
            args.PushMarkup(IsLinkedToLiveBracelet(ent)
                ? Loc.GetString("rmc-pdt-locator-examine-linked")
                : Loc.GetString("rmc-pdt-locator-examine-unlinked"));
        }
    }

    private void OnBraceletExamined(Entity<PDTBraceletComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(PDTBraceletComponent)))
        {
            args.PushMarkup(Loc.GetString("rmc-pdt-examine-serial", ("serial", GetSerial(ent.Comp))));
            args.PushMarkup(IsLinkedToLiveLocator(ent)
                ? Loc.GetString("rmc-pdt-bracelet-examine-linked")
                : Loc.GetString("rmc-pdt-bracelet-examine-unlinked"));
        }
    }

    private bool TryPairWithDelay(Entity<PDTLocatorComponent> locator, Entity<PDTBraceletComponent> bracelet, EntityUid user)
    {
        UseDelayComponent? useDelay = null;
        if (TryComp(locator, out useDelay) &&
            _useDelay.IsDelayed((locator.Owner, useDelay)))
        {
            return false;
        }

        var paired = TryPair(locator, bracelet, user);

        if (useDelay != null)
            _useDelay.TryResetDelay((locator.Owner, useDelay));

        return paired;
    }

    private bool TryPair(Entity<PDTLocatorComponent> locator, Entity<PDTBraceletComponent> bracelet, EntityUid user)
    {
        if (locator.Comp.LinkedBracelet == bracelet.Owner &&
            bracelet.Comp.LinkedLocator == locator.Owner)
        {
            _popup.PopupEntity(Loc.GetString("rmc-pdt-pair-already"), locator, user);
            return false;
        }

        if (IsLinkedToLiveBracelet(locator) || IsLinkedToLiveLocator(bracelet))
        {
            _popup.PopupEntity(Loc.GetString("rmc-pdt-pair-busy"), locator, user, PopupType.SmallCaution);
            return false;
        }

        Link(locator, bracelet);
        _popup.PopupEntity(Loc.GetString("rmc-pdt-pair-success", ("serial", GetSerial(locator.Comp))), locator, user);
        UpdateLocatorVisuals(locator);
        return true;
    }

    private void Link(Entity<PDTLocatorComponent> locator, Entity<PDTBraceletComponent> bracelet)
    {
        var serial = locator.Comp.Serial ?? bracelet.Comp.Serial ?? GenerateSerial();
        locator.Comp.LinkedBracelet = bracelet.Owner;
        locator.Comp.Serial = serial;
        bracelet.Comp.LinkedLocator = locator.Owner;
        bracelet.Comp.Serial = serial;
        UpdateLocatorVisuals(locator);
    }

    private bool TryGetSignal(
        Entity<PDTLocatorComponent> locator,
        out Entity<PDTBraceletComponent> bracelet,
        out MapCoordinates locatorCoords,
        out MapCoordinates braceletCoords,
        EntityUid? user)
    {
        locatorCoords = default;
        braceletCoords = default;
        bracelet = default;

        if (locator.Comp.LinkedBracelet is not { } braceletUid ||
            !_braceletQuery.TryComp(braceletUid, out var braceletComp) ||
            TerminatingOrDeleted(braceletUid))
        {
            locator.Comp.LinkedBracelet = null;
            UpdateLocatorVisuals(locator);
            if (user != null)
                _popup.PopupEntity(Loc.GetString("rmc-pdt-locator-no-link"), locator, user.Value, PopupType.SmallCaution);

            return false;
        }

        locatorCoords = _transform.GetMapCoordinates(locator.Owner);
        braceletCoords = _transform.GetMapCoordinates(braceletUid);
        if (locatorCoords.MapId != braceletCoords.MapId)
        {
            if (user != null)
                _popup.PopupEntity(Loc.GetString("rmc-pdt-locator-no-signal"), locator, user.Value, PopupType.SmallCaution);

            return false;
        }

        bracelet = (braceletUid, braceletComp);
        return true;
    }

    private void UpdateLocatorVisuals(Entity<PDTLocatorComponent> locator)
    {
        var screen = GetLocatorScreen(locator);
        var bracelet = screen == PDTLocatorScreenVisuals.Off
            ? PDTLocatorBraceletVisuals.Hidden
            : IsLinkedToLiveBracelet(locator)
                ? PDTLocatorBraceletVisuals.Linked
                : PDTLocatorBraceletVisuals.Unlinked;

        _appearance.SetData(locator.Owner, PDTLocatorVisuals.Screen, screen);
        _appearance.SetData(locator.Owner, PDTLocatorVisuals.Bracelet, bracelet);
    }

    private PDTLocatorScreenVisuals GetLocatorScreen(Entity<PDTLocatorComponent> locator)
    {
        if (!_cell.TryGetBatteryFromSlot(locator, out _, out var battery) ||
            battery.MaxCharge <= 0 ||
            battery.CurrentCharge <= 0)
        {
            return PDTLocatorScreenVisuals.Off;
        }

        if (battery.CurrentCharge < locator.Comp.PingCharge)
            return PDTLocatorScreenVisuals.Red;

        if (battery.CurrentCharge < battery.MaxCharge * LowBatteryFraction)
            return PDTLocatorScreenVisuals.Yellow;

        return PDTLocatorScreenVisuals.Normal;
    }

    private bool IsLinkedToLiveBracelet(Entity<PDTLocatorComponent> locator)
    {
        return locator.Comp.LinkedBracelet is { } linked &&
               !TerminatingOrDeleted(linked) &&
               _braceletQuery.HasComp(linked);
    }

    private bool IsLinkedToLiveLocator(Entity<PDTBraceletComponent> bracelet)
    {
        return bracelet.Comp.LinkedLocator is { } linked &&
               !TerminatingOrDeleted(linked) &&
               _locatorQuery.HasComp(linked);
    }

    private string GetSerial(PDTLocatorComponent locator)
    {
        return locator.Serial ?? Loc.GetString("rmc-pdt-serial-unset");
    }

    private string GetSerial(PDTBraceletComponent bracelet)
    {
        return bracelet.Serial ?? Loc.GetString("rmc-pdt-serial-unset");
    }

    private string GenerateSerial()
    {
        return $"PDTL-{_random.Next(1000, 10000)}";
    }

    private static string GetDirection(Vector2 delta)
    {
        return delta.ToWorldAngle().GetDir().GetShorthand();
    }
}

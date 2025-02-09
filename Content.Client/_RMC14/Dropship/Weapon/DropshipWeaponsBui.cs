﻿using System.Text;
using Content.Client.Eye;
using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RMC14.Dropship.AttachmentPoint;
using Content.Shared._RMC14.Dropship.Weapon;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;
using static Content.Shared._RMC14.Dropship.Weapon.DropshipTerminalWeaponsComponent;
using static Content.Shared._RMC14.Dropship.Weapon.DropshipTerminalWeaponsScreen;
using static Robust.Client.UserInterface.Control;
using static Robust.Client.UserInterface.Controls.BaseButton;
using MedevacComponent = Content.Shared._RMC14.Dropship.Utility.Components.MedevacComponent;

namespace Content.Client._RMC14.Dropship.Weapon;

[UsedImplicitly]
public sealed class DropshipWeaponsBui : BoundUserInterface
{
    private DropshipWeaponsWindow? _window;

    private readonly ContainerSystem _container;
    private readonly EyeLerpingSystem _eyeLerping;
    private readonly DropshipSystem _system;
    private readonly DropshipWeaponSystem _weaponSystem;

    private EntityUid? _oldEye;

    public DropshipWeaponsBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _container = EntMan.System<ContainerSystem>();
        _eyeLerping = EntMan.System<EyeLerpingSystem>();
        _system = EntMan.System<DropshipSystem>();
        _weaponSystem = EntMan.System<DropshipWeaponSystem>();
    }

    protected override void Open()
    {
        _window = new DropshipWeaponsWindow();
        _window.OnClose += Close;

        _window.OffsetUpButton.Text = "^";
        _window.OffsetUpButton.OnPressed += _ =>
            SendPredictedMessage(new DropshipTerminalWeaponsAdjustOffsetMsg(Direction.North));

        _window.OffsetLeftButton.Text = "<";
        _window.OffsetLeftButton.OnPressed += _ =>
            SendPredictedMessage(new DropshipTerminalWeaponsAdjustOffsetMsg(Direction.West));

        _window.OffsetRightButton.Text = ">";
        _window.OffsetRightButton.OnPressed += _ =>
            SendPredictedMessage(new DropshipTerminalWeaponsAdjustOffsetMsg(Direction.East));

        _window.OffsetDownButton.Text = "v";
        _window.OffsetDownButton.OnPressed += _ =>
            SendPredictedMessage(new DropshipTerminalWeaponsAdjustOffsetMsg(Direction.South));

        _window.ResetOffsetButton.OnPressed += _ =>
            SendPredictedMessage(new DropshipTerminalWeaponsResetOffsetMsg());

        _window.ScreenOne.TopRow.Refresh();
        _window.ScreenOne.LeftRow.Refresh();
        _window.ScreenOne.RightRow.Refresh();
        _window.ScreenOne.BottomRow.Refresh();

        _window.ScreenTwo.TopRow.Refresh();
        _window.ScreenTwo.LeftRow.Refresh();
        _window.ScreenTwo.RightRow.Refresh();
        _window.ScreenTwo.BottomRow.Refresh();

        Refresh();

        _window.OpenCentered();
    }

    public void Refresh()
    {
        if (_window is not { Disposed: false })
            return;

        if (EntMan.TryGetComponent(Owner, out DropshipTerminalWeaponsComponent? terminal))
        {
            SetScreen(true, terminal.ScreenOne);
            SetScreen(false, terminal.ScreenTwo);
        }

        RefreshButtons();
    }

    private void RefreshButtons()
    {
        if (_window is not { Disposed: false })
            return;

        foreach (var button in _window.GetControlOfType<DropshipWeaponsButton>())
        {
            button.Refresh();
        }
    }

    private void SetScreen(bool first, Screen compScreen)
    {
        if (_window is not { Disposed: false } ||
            !EntMan.TryGetComponent(Owner, out DropshipTerminalWeaponsComponent? terminal))
        {
            return;
        }

        var screen = first ? _window.ScreenOne : _window.ScreenTwo;
        static DropshipWeaponsButtonData ButtonAction(string suffix, Action<ButtonEventArgs> onPressed)
        {
            return new DropshipWeaponsButtonData($"rmc-dropship-weapons-{suffix}", onPressed);
        }

        DropshipWeaponsButtonData Button(string suffix, DropshipTerminalWeaponsScreen change)
        {
            var msg = new DropshipTerminalWeaponsChangeScreenMsg(first, change);
            void OnPressed(ButtonEventArgs _) => SendPredictedMessage(msg);
            return ButtonAction(suffix, OnPressed);
        }

        string TargetAcquisition()
        {
            var weapon = EntMan.GetEntity(compScreen.Weapon);
            return Loc.GetString("rmc-dropship-weapons-target-strike",
                ("mode", compScreen.Weapon == null ? "NONE" : "WEAPON"),
                ("weapon", weapon == null ? "" : weapon),
                ("target", terminal.Target == null ? "NONE" : terminal.Target.Value),
                ("xOffset", terminal.Offset.X),
                ("yOffset", terminal.Offset.Y));
        }

        void AddTargets(
            out DropshipWeaponsButtonData? previous,
            out DropshipWeaponsButtonData? next)
        {
            previous = default;
            next = default;

            var firstTarget = terminal.TargetsPage * 5;
            var targets = terminal.Targets;
            if (targets.Count <= 5)
                firstTarget = 0;
            else if (firstTarget > targets.Count - 5)
                firstTarget = targets.Count - 5;

            DropshipWeaponsButtonData? GetTargetData(int index)
            {
                if (!targets.TryGetValue(index, out var target))
                    return null;

                var msg = new DropshipTerminalWeaponsTargetsSelectMsg(target.Id);
                return new DropshipWeaponsButtonData(target.Name, _ => SendPredictedMessage(msg));
            }

            if (firstTarget > 0)
                previous = ButtonAction("previous", _ => SendPredictedMessage(new DropshipTerminalWeaponsTargetsPreviousMsg()));

            if (firstTarget + 4 < targets.Count - 1)
                next = ButtonAction("next", _ => SendPredictedMessage(new DropshipTerminalWeaponsTargetsNextMsg()));

            var one = GetTargetData(firstTarget);
            var two = GetTargetData(firstTarget + 1);
            var three = GetTargetData(firstTarget + 2);
            var four = GetTargetData(firstTarget + 3);
            var five = GetTargetData(firstTarget + 4);
            screen.RightRow.SetData(one, two, three, four, five);
        }

        void AddMedevacs(
            out DropshipWeaponsButtonData? previous,
            out DropshipWeaponsButtonData? next)
        {
            var firstTarget = terminal.MedevacsPage * 5;
            var targets = terminal.Medevacs;
            if (targets.Count <= 5)
                firstTarget = 0;
            else if (firstTarget > targets.Count - 5)
                firstTarget = targets.Count - 5;

            DropshipWeaponsButtonData? GetTargetData(int index)
            {
                if (!targets.TryGetValue(index, out var target))
                    return null;

                var msg = new DropshipTerminalWeaponsMedevacSelectMsg(target.Id);
                return new DropshipWeaponsButtonData(target.Name, _ => SendPredictedMessage(msg));
            }

            previous = default;
            if (firstTarget > 0)
                previous = ButtonAction("previous", _ => SendPredictedMessage(new DropshipTerminalWeaponsMedevacPreviousMsg()));

            next = default;
            if (firstTarget + 4 < targets.Count - 1)
                next = ButtonAction("next", _ => SendPredictedMessage(new DropshipTerminalWeaponsMedevacNextMsg()));

            var one = GetTargetData(firstTarget);
            var two = GetTargetData(firstTarget + 1);
            var three = GetTargetData(firstTarget + 2);
            var four = GetTargetData(firstTarget + 3);
            var five = GetTargetData(firstTarget + 4);
            screen.LeftRow.SetData(one, two, three, four, five);
        }

        var equip = Button("equip", Equip);
        // var fireMission = Loc.GetString("rmc-dropship-weapons-fire-mission");
        var exit = ButtonAction("exit", _ => SendPredictedMessage(new DropshipTerminalWeaponsExitMsg(first)));
        var target = Button("target", Target);
        // var maps = Button("equip", DropshipTerminalWeaponsScreen.Maps);
        var cams = Button("cams", Cams);
        var fire = ButtonAction("fire", _ => SendPredictedMessage(new DropshipTerminalWeaponsFireMsg(first)));
        var strike = Button("strike", Strike);
        // var vector = Loc.GetString("rmc-dropship-weapons-vector");
        var nightVisionOn = ButtonAction("night-vision-on",
            _ => SendPredictedMessage(new DropshipTerminalWeaponsNightVisionMsg(true)));
        var nightVisionOff = ButtonAction("night-vision-off",
            _ => SendPredictedMessage(new DropshipTerminalWeaponsNightVisionMsg(false)));
        var cancel = ButtonAction("cancel", _ => SendPredictedMessage(new DropshipTerminalWeaponsCancelMsg(first)));
        var weapon = Button("weapon", StrikeWeapon);

        screen.ScreenLabel.Text = Loc.GetString("rmc-dropship-weapons-main-screen-text");
        screen.ScreenLabel.VerticalAlignment = VAlignment.Stretch;
        screen.ScreenLabel.Margin = new Thickness();
        screen.ScreenLabel.Visible = true;

        screen.Viewport.Visible = false;

        ClearNames(screen);
        switch (compScreen.State)
        {
            case Main:
                // TODO RMC14 bottom two maps
                screen.BottomRow.SetData(three: cams);
                screen.TopRow.SetData(equip, four: target);
                break;
            case Equip:
            {
                screen.BottomRow.SetData(exit);
                TryGetWeapons(
                    first,
                    out var one,
                    out var two,
                    out var three,
                    out var four,
                    out var utilityOne,
                    out var utilityTwo,
                    out var utilityThree
                );
                screen.LeftRow.SetData(one, two, utilityOne, utilityTwo, utilityThree);
                screen.RightRow.SetData(three, four);

                var text = new StringBuilder();
                void AddWeaponEntry(DropshipWeaponsButtonData? data)
                {
                    if (data?.Weapon is not { } netWeapon ||
                        !EntMan.TryGetEntity(netWeapon, out var weaponEnt))
                    {
                        return;
                    }

                    var rounds = _weaponSystem.GetWeaponRounds(weaponEnt.Value);
                    text.AppendLine(Loc.GetString("rmc-dropship-weapons-equip-weapon-ammo",
                        ("weapon", weaponEnt),
                        ("rounds", rounds)));
                    text.AppendLine();
                }

                AddWeaponEntry(one);
                AddWeaponEntry(two);
                AddWeaponEntry(three);
                AddWeaponEntry(four);

                screen.ScreenLabel.Text = text.ToString();
                screen.ScreenLabel.VerticalAlignment = VAlignment.Top;
                screen.ScreenLabel.Margin = new Thickness(10);
                break;
            }
            case Target:
            {
                AddTargets(out var previous, out var next);
                screen.BottomRow.SetData(exit, five: next);
                screen.TopRow.SetData(fire, five: previous);
                // TODO RMC14 left two vector
                screen.LeftRow.SetData(strike);
                screen.ScreenLabel.Text = TargetAcquisition();
                break;
            }
            case Strike:
            {
                AddTargets(out var previous, out var next);
                screen.BottomRow.SetData(exit, five: next);
                screen.TopRow.SetData(fire, five: previous);
                screen.LeftRow.SetData(cancel, weapon);
                screen.ScreenLabel.Text = TargetAcquisition();
                break;
            }
            case StrikeWeapon:
            {
                AddTargets(out var previous, out var next);
                screen.BottomRow.SetData(exit, five: next);
                screen.TopRow.SetData(fire, five: previous);
                TryGetWeapons(first, out var one, out var two, out var three, out var four, out _, out _, out _);
                screen.LeftRow.SetData(cancel, one, two, three, four);
                screen.ScreenLabel.Text = TargetAcquisition();
                break;
            }
            case SelectingWeapon:
                screen.ScreenLabel.VerticalAlignment = VAlignment.Top;
                screen.ScreenLabel.Margin = new Thickness(0, 10);
                if (EntMan.TryGetEntity(compScreen.Weapon, out var selectedWeapon))
                {
                    if (_weaponSystem.TryGetWeaponAmmo(selectedWeapon.Value, out var ammo))
                    {
                        screen.ScreenLabel.Text = Loc.GetString("rmc-dropship-weapons-weapon-selected-ammo",
                            ("weapon", selectedWeapon.Value),
                            ("ammo", ammo),
                            ("rounds", ammo.Comp.Rounds),
                            ("maxRounds", ammo.Comp.MaxRounds));
                    }
                    else
                    {
                        screen.ScreenLabel.Text = Loc.GetString("rmc-dropship-weapons-weapon-selected",
                            ("weapon", selectedWeapon.Value));
                    }
                }

                screen.TopRow.SetData(equip);
                screen.LeftRow.SetData(fire);
                screen.BottomRow.SetData(exit);
                break;
            case Cams:
                screen.LeftRow.SetData(nightVisionOn, nightVisionOff);
                screen.ScreenLabel.Visible = false;

                if (_oldEye != null && _oldEye != terminal.Target)
                    _eyeLerping.RemoveEye(_oldEye.Value);

                _oldEye = terminal.Target;
                if (terminal.Target != null &&
                    _weaponSystem.TryGetTargetEye((Owner, terminal), terminal.Target.Value, out var eyeId))
                {
                    if (!EntMan.HasComponent<LerpingEyeComponent>(eyeId))
                        _eyeLerping.AddEye(eyeId);

                    if (EntMan.TryGetComponent(eyeId, out EyeComponent? eye))
                        screen.Viewport.Eye = eye.Eye;
                }

                screen.Viewport.Visible = true;
                screen.BottomRow.SetData(exit);
                break;
            case Medevac:
            {
                AddMedevacs(out var previous, out var next);
                screen.TopRow.SetData(equip);
                screen.BottomRow.SetData(exit);
                screen.RightRow.SetData(one: previous, five: next);
                break;
            }
            default:
                screen.BottomRow.SetData(exit);
                break;
        }

        RefreshButtons();
    }

    private void ClearNames(DropshipWeaponsScreen screen)
    {
        screen.TopRow.SetData();
        screen.LeftRow.SetData();
        screen.RightRow.SetData();
        screen.BottomRow.SetData();
    }

    private void TryGetWeapons(
        bool first,
        out DropshipWeaponsButtonData? one,
        out DropshipWeaponsButtonData? two,
        out DropshipWeaponsButtonData? three,
        out DropshipWeaponsButtonData? four,
        out DropshipWeaponsButtonData? utilityOne,
        out DropshipWeaponsButtonData? utilityTwo,
        out DropshipWeaponsButtonData? utilityThree)
    {
        one = default;
        two = default;
        three = default;
        four = default;
        utilityOne = default;
        utilityTwo = default;
        utilityThree = default;
        if (!_system.TryGetGridDropship(Owner, out var dropship))
            return;

        var weapons = new List<DropshipWeaponsButtonData?>();
        var utility = new List<DropshipWeaponsButtonData?>();
        foreach (var pointId in dropship.Comp.AttachmentPoints)
        {
            if (EntMan.TryGetComponent(pointId, out DropshipUtilityPointComponent? utilityComp) &&
                _container.TryGetContainer(pointId, utilityComp.UtilitySlotId, out var utilityContainer) &&
                utilityContainer.ContainedEntities.Count > 0)
            {
                var utilityMount = utilityContainer.ContainedEntities[0];
                if (!EntMan.HasComponent<MedevacComponent>(utilityMount))
                    continue;

                var netEnt = EntMan.GetNetEntity(utilityMount);
                var msg = new DropshipTerminalWeaponsChooseMedevacMsg(first);
                var data = new DropshipWeaponsButtonData(
                    "Medeva",
                    _ => SendPredictedMessage(msg),
                    netEnt
                );
                utility.Add(data);
            }

            if (!EntMan.TryGetComponent(pointId, out DropshipWeaponPointComponent? pointComp))
                continue;

            if (!_container.TryGetContainer(pointId, pointComp.WeaponContainerSlotId, out var container))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!EntMan.TryGetComponent(contained, out DropshipWeaponComponent? weapon))
                    continue;

                var netEnt = EntMan.GetNetEntity(contained);
                var msg = new DropshipTerminalWeaponsChooseWeaponMsg(first, netEnt);
                var data = new DropshipWeaponsButtonData(
                    weapon.Abbreviation,
                    _ => SendPredictedMessage(msg),
                    netEnt
                );
                weapons.Add(data);

                if (weapons.Count >= 4)
                    break;
            }
        }

        weapons.TryGetValue(0, out one);
        weapons.TryGetValue(1, out two);
        weapons.TryGetValue(2, out three);
        weapons.TryGetValue(3, out four);

        utility.TryGetValue(0, out utilityOne);
        utility.TryGetValue(1, out utilityTwo);
        utility.TryGetValue(2, out utilityThree);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}

using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RMC14.Dropship;
using Content.Shared._RMC14.Dropship.Fabricator;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Dropship.Weapons;

[UsedImplicitly]
public sealed class DropshipWeaponsBui : BoundUserInterface
{
    private DropshipWeaponsWindow? _window;

    private ContainerSystem _container;
    private DropshipSystem _system;

    public DropshipWeaponsBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _container = EntMan.System<ContainerSystem>();
        _system = EntMan.System<DropshipSystem>();
    }

    protected override void Open()
    {
        _window = new DropshipWeaponsWindow();
        _window.OnClose += Close;

        _window.OffsetUpButton.Text = "^";
        _window.OffsetLeftButton.Text = "<";
        _window.OffsetRightButton.Text = ">";
        _window.OffsetDownButton.Text = "v";
        _window.OffsetCalibrationLabel.Text = "Offset\nCalibration";

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

    private void Refresh()
    {
        if (_window is not { Disposed: false })
            return;

        if (EntMan.TryGetComponent(Owner, out DropshipTerminalWeaponsComponent? terminal))
        {
            SetScreen(_window.ScreenOne, terminal.ScreenOne);
            SetScreen(_window.ScreenTwo, terminal.ScreenTwo);
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

    private void SetScreen(DropshipWeaponsScreen screen, DropshipTerminalWeaponsScreen state)
    {
        var equip = Loc.GetString("rmc-dropship-weapons-equip");
        var fireMission = Loc.GetString("rmc-dropship-weapons-fire-mission");
        var exit = Loc.GetString("rmc-dropship-weapons-exit");
        var target = Loc.GetString("rmc-dropship-weapons-target");
        var maps = Loc.GetString("rmc-dropship-weapons-maps");
        var cams = Loc.GetString("rmc-dropship-weapons-cams");
        var fire = Loc.GetString("rmc-dropship-weapons-fire");
        var strike = Loc.GetString("rmc-dropship-weapons-strike");
        var vector = Loc.GetString("rmc-dropship-weapons-vector");
        var nightVisionOn = Loc.GetString("rmc-dropship-weapons-night-vision-on");
        var nightVisionOff = Loc.GetString("rmc-dropship-weapons-night-vision-off");
        var cancel = Loc.GetString("rmc-dropship-weapons-cancel");
        var weapon = Loc.GetString("rmc-dropship-weapons-weapon");

        ClearNames(screen);
        switch (state)
        {
            case DropshipTerminalWeaponsScreen.Main:
                screen.BottomRow.SetNames(equip, two: maps, three: cams);
                screen.TopRow.SetNames(equip, four: target);
                break;
            case DropshipTerminalWeaponsScreen.Equip:
            {
                screen.BottomRow.SetNames(exit);
                TryGetWeapons(out var one, out var two, out var three, out var four);
                screen.LeftRow.SetNames(one, two, three, four);
                break;
            }
            case DropshipTerminalWeaponsScreen.Target:
                screen.BottomRow.SetNames(exit);
                screen.TopRow.SetNames(fire);
                screen.LeftRow.SetNames(strike, vector);
                break;
            case DropshipTerminalWeaponsScreen.Strike:
                screen.BottomRow.SetNames(exit);
                screen.TopRow.SetNames(fire);
                screen.LeftRow.SetNames(cancel, weapon);
                break;
            case DropshipTerminalWeaponsScreen.Weapon:
            {
                screen.BottomRow.SetNames(exit);
                screen.TopRow.SetNames(fire);
                TryGetWeapons(out var one, out var two, out var three, out var four);
                screen.LeftRow.SetNames(cancel, one, two, three, four);
                break;
            }
        }

        RefreshButtons();
    }

    private void ClearNames(DropshipWeaponsScreen screen)
    {
        screen.TopRow.SetNames();
        screen.LeftRow.SetNames();
        screen.RightRow.SetNames();
        screen.BottomRow.SetNames();
    }

    private void TryGetWeapons(out string? one, out string? two, out string? three, out string? four)
    {
        one = default;
        two = default;
        three = default;
        four = default;
        if (!_system.TryGetGridDropship(Owner, out var dropship))
            return;

        var names = new List<string>();
        foreach (var pointId in dropship.Comp.AttachmentPoints)
        {
            if (!EntMan.TryGetComponent(pointId, out DropshipWeaponPointComponent? pointComp))
                continue;

            if (!_container.TryGetContainer(pointId, pointComp.ContainerId, out var container))
                continue;

            foreach (var contained in container.ContainedEntities)
            {
                if (!EntMan.TryGetComponent(contained, out DropshipWeaponComponent? weapon))
                    continue;

                names.Add(weapon.Abbreviation);
                if (names.Count >= 4)
                    break;
            }
        }

        names.TryGetValue(0, out one);
        names.TryGetValue(1, out two);
        names.TryGetValue(2, out three);
        names.TryGetValue(3, out four);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
            _window?.Dispose();
    }
}

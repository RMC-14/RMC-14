using Content.Shared._RMC14.Chemistry.ChemMaster;
using Content.Shared._RMC14.Medical.HUD;
using FastAccessors;
using JetBrains.Annotations;
using Robust.Client.UserInterface.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Client._RMC14.Chemistry.PillBottle;

[UsedImplicitly]
public sealed partial class RMCChangePillBottleColorBui : BoundUserInterface
{
    private RMCChangePillBottleColorWindow? _window;
    private List<string> _selectableColorNames = new();
    private int _selectedColor = -1;
    public RMCChangePillBottleColorBui(EntityUid owner, Enum key) : base(owner, key)
    {
    }

    protected override void Open()
    {
        _window = new RMCChangePillBottleColorWindow(this);
        _window.OnClose += Close;
        _window.OpenCentered();

        var colorNames = Enum.GetNames<PillbottleColor>();

        _selectableColorNames = new(colorNames);

        var searchBar = _window.SearchBar;
        var colorList = _window.SelectableColors;

        colorList.OnItemSelected += OnPillbottleColorSelect;
        colorList.OnItemDeselected += OnPillbottleColorDeselect;

        searchBar.OnTextChanged += (_) => UpdatePillbottleColorsList(searchBar.Text);
        UpdatePillbottleColorsList();
    }

    private void OnPillbottleColorSelect(ItemList.ItemListSelectedEventArgs args)
    {
        var newSelectedColor = (PillbottleColor)args.ItemList[args.ItemIndex].Metadata!;

        if (newSelectedColor is { } newColor)
        {
            ChangeColor(newColor);
            Close();
        }
    }

    private void OnPillbottleColorDeselect(ItemList.ItemListDeselectedEventArgs args)
    {

    }

    private void UpdatePillbottleColorsList(string? filter = null)
    {
        if (_window is null)
        {
            return;
        }
        var colorList = _window.SelectableColors;
        colorList.Clear();

        for (var i = 0; i < _selectableColorNames.Count; i++)
        {
            var colorName = _selectableColorNames[i];
            if (!string.IsNullOrEmpty(filter) &&
                !colorName.ToLowerInvariant().Contains(filter.Trim().ToLowerInvariant()))
            {
                continue;
            }

            ItemList.Item listEntry = new(colorList)
            {
                Text = colorName.Replace('_', ' '),
                Metadata = (Byte)Enum.Parse<PillbottleColor>(colorName, false),
            };

            colorList.Add(listEntry);
        }
    }

    private void ChangeColor(PillbottleColor newColor)
    {
        SendMessage(new ChangePillBottleColorMessage(newColor));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!disposing)
            return;

        _window?.Dispose();
    }
}

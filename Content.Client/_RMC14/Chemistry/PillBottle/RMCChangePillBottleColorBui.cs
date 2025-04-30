using Content.Shared._RMC14.Chemistry.ChemMaster;
using JetBrains.Annotations;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._RMC14.Chemistry.PillBottle;

[UsedImplicitly]
public sealed class RMCChangePillBottleColorBui(EntityUid owner, Enum key) : BoundUserInterface(owner, key)
{
    private RMCChangePillBottleColorWindow? _window;
    private List<string> _selectableColorNames = new();

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<RMCChangePillBottleColorWindow>();

        var colorNames = Enum.GetNames<PillbottleColor>();

        _selectableColorNames = new List<string>(colorNames);

        var searchBar = _window.SearchBar;
        var colorList = _window.SelectableColors;

        colorList.OnItemSelected += OnPillbottleColorSelect;

        searchBar.OnTextChanged += (_) => UpdatePillbottleColorsList(searchBar.Text);
        UpdatePillbottleColorsList();
    }

    private void OnPillbottleColorSelect(ItemList.ItemListSelectedEventArgs args)
    {
        var newSelectedColor = (PillbottleColor)args.ItemList[args.ItemIndex].Metadata!;

        if (newSelectedColor is var newColor)
        {
            ChangeColor(newColor);
            Close();
        }
    }

    private void UpdatePillbottleColorsList(string? filter = null)
    {
        if (_window is null)
        {
            return;
        }
        var colorList = _window.SelectableColors;
        colorList.Clear();

        foreach (var colorName in _selectableColorNames)
        {
            if (!string.IsNullOrEmpty(filter) &&
                !colorName.ToLowerInvariant().Contains(filter.Trim().ToLowerInvariant()))
            {
                continue;
            }

            ItemList.Item listEntry = new(colorList)
            {
                Text = colorName.Replace('_', ' '),
                Metadata = (byte) Enum.Parse<PillbottleColor>(colorName, false),
            };

            colorList.Add(listEntry);
        }
    }

    private void ChangeColor(PillbottleColor newColor)
    {
        SendMessage(new ChangePillBottleColorMessage(newColor));
    }
}

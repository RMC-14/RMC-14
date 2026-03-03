using System.Numerics;
using Content.Client._RMC14.Xenonids.UI;
using Content.Shared._RMC14.Xenonids.Watch;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.LineEdit;

namespace Content.Client._RMC14.Xenonids.HiveTeam;

public sealed class XenoPickerWindow : DefaultWindow
{
    private readonly BoxContainer _container;
    private readonly LineEdit _searchBar;
    private readonly List<(XenoChoiceControl Control, string Name)> _entries = new();

    public XenoPickerWindow(
        List<Xeno> xenos,
        Func<EntProtoId?, Texture?> getTexture,
        Action<NetEntity> onPicked)
    {
        Title = "Select Xenonid";
        SetSize = new Vector2(300, 400);

        var vbox = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        Contents.AddChild(vbox);

        _searchBar = new LineEdit { PlaceHolder = "Search..." };
        _searchBar.OnTextChanged += OnSearch;
        vbox.AddChild(_searchBar);

        var scroll = new ScrollContainer { HScrollEnabled = false, HorizontalExpand = true, VerticalExpand = true };
        _container = new BoxContainer { Orientation = BoxContainer.LayoutOrientation.Vertical };
        scroll.AddChild(_container);
        vbox.AddChild(scroll);

        foreach (var xeno in xenos)
        {
            var texture = getTexture(xeno.Id);
            var control = new XenoChoiceControl();
            control.Set(xeno.Name, texture);
            var captured = xeno;
            control.Button.OnPressed += _ =>
            {
                onPicked(captured.Entity);
                Close();
            };
            _container.AddChild(control);
            _entries.Add((control, xeno.Name));
        }
    }

    private void OnSearch(LineEditEventArgs args)
    {
        foreach (var (control, name) in _entries)
        {
            control.Visible = string.IsNullOrWhiteSpace(args.Text) ||
                              name.Contains(args.Text, StringComparison.OrdinalIgnoreCase);
        }
    }
}

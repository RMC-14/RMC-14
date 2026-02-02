using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RMC14.Telephone;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Telephone;

public sealed class RMCTelephoneBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private static readonly List<string> TabOrder = new() { "MP Dept.", "Almayer", "Command", "Offices", "ARES", "Dropship", "Marine" };

    private TelephoneWindow? _window;

    protected override void Open()
    {
        base.Open();
        _window = this.CreateWindow<TelephoneWindow>();

        if (EntMan.TryGetComponent(Owner, out MetaDataComponent? metaData))
            _window.Title = metaData.EntityName;

        Refresh();
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (_window is not { IsOpen: true })
            return;

        if (State is not RMCTelephoneBuiState state)
            return;

        _window.Tabs.DisposeAllChildren();
        var tabs = new Dictionary<string, BoxContainer>();
        foreach (var phone in state.Phones)
        {
            if (!tabs.TryGetValue(phone.Category, out var tab))
            {
                tab = new BoxContainer { Orientation = LayoutOrientation.Vertical };
                tabs[phone.Category] = tab;

                var scroll = new ScrollContainer
                {
                    HScrollEnabled = false,
                    VScrollEnabled = true,
                    VerticalExpand = true,
                };

                var category = new BoxContainer { Orientation = LayoutOrientation.Vertical };
                scroll.AddChild(category);

                var searchBar = new LineEdit();
                tab.AddChild(searchBar);
                tab.AddChild(scroll);

                searchBar.OnTextChanged += args =>
                {
                    foreach (var scroll in _window.Tabs.GetControlOfType<ScrollContainer>())
                    {
                        foreach (var container in scroll.GetControlOfType<BoxContainer>())
                        {
                            foreach (var child in container.Children)
                            {
                                if (child is LineEdit otherBar)
                                {
                                    otherBar.SetText(args.Text, false);
                                }
                                else if (child is Button button)
                                {
                                    button.Visible = button.Text?.Contains(args.Text, StringComparison.OrdinalIgnoreCase) ?? false;
                                }
                            }
                        }
                    }
                };
            }

            foreach (var child in tab.Children)
            {
                if (child is not ScrollContainer scroll)
                    continue;

                foreach (var scrollChild in scroll.Children)
                {
                    if (scrollChild is not BoxContainer category)
                        continue;

                    var phoneButton = new Button
                    {
                        Text = phone.Name,
                        StyleClasses = { "OpenBoth" },
                    };
                    phoneButton.OnPressed += _ => SendPredictedMessage(new RMCTelephoneCallBuiMsg(phone.Id));
                    category.AddChild(phoneButton);
                    break;
                }
            }
        }

        foreach (var categoryName in TabOrder)
        {
            if (tabs.Remove(categoryName, out var category))
            {
                _window.Tabs.AddChild(category);
                TabContainer.SetTabTitle(category, categoryName);
            }
        }

        foreach (var (categoryName, category) in tabs)
        {
            _window.Tabs.AddChild(category);
            TabContainer.SetTabTitle(category, categoryName);
        }

        _window.Buttons.DisposeAllChildren();
        if (state.Dnd)
        {
            var disableDndButton = new Button
            {
                Text = Loc.GetString("phone-dnd-button"),
                StyleClasses = { "OpenBoth", "Caution" },
                ToolTip = Loc.GetString("phone-dnd-tooltip-enabled"),
            };
            disableDndButton.OnPressed += _ => SendPredictedMessage(new RMCTelephoneDndBuiMsg(false));
            _window.Buttons.AddChild(disableDndButton);
        }
        else if (state.CanDnd)
        {
            var enableDndButton = new Button
            {
                Text = Loc.GetString("phone-dnd-button"),
                StyleClasses = { "OpenBoth" },
                ToolTip = Loc.GetString("phone-dnd-tooltip-disabled"),
            };
            enableDndButton.OnPressed += _ => SendPredictedMessage(new RMCTelephoneDndBuiMsg(true));
            _window.Buttons.AddChild(enableDndButton);
        }
        else
        {
            var enableDndButton = new Button
            {
                Text = Loc.GetString("phone-dnd-button"),
                StyleClasses = { "OpenBoth" },
                ToolTip = Loc.GetString("phone-dnd-tooltip-locked"),
                Disabled = true,
            };
            _window.Buttons.AddChild(enableDndButton);
        }
    }
}

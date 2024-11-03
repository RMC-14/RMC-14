using Content.Client.UserInterface.ControlExtensions;
using Content.Shared._RMC14.Telephone;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Telephone;

public sealed class TelephoneBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private TelephoneWindow? _window;

    protected override void Open()
    {
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

        if (State is not TelephoneBuiState state)
            return;

        _window.Tabs.DisposeAllChildren();
        var categories = new Dictionary<string, BoxContainer>();
        foreach (var phone in state.Phones)
        {
            if (!categories.TryGetValue(phone.Category, out var category))
            {
                var tab = new BoxContainer { Orientation = LayoutOrientation.Vertical };

                var scroll = new ScrollContainer
                {
                    HScrollEnabled = false,
                    VScrollEnabled = true,
                    VerticalExpand = true,
                };

                category = new BoxContainer { Orientation = LayoutOrientation.Vertical };
                scroll.AddChild(category);
                categories[phone.Category] = category;

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

                _window.Tabs.AddChild(tab);
                TabContainer.SetTabTitle(tab, phone.Category);
            }

            var phoneButton = new Button
            {
                Text = phone.Name,
                StyleClasses = { "OpenBoth" },
            };
            phoneButton.OnPressed += _ => SendPredictedMessage(new TelephoneCallBuiMsg(phone.Id));
            category.AddChild(phoneButton);
        }
    }
}

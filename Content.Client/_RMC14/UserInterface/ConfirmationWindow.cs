using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;

namespace Content.Client._RMC14.UserInterface;

public sealed class ConfirmationWindow : DefaultWindow
{
    public readonly Button AcceptButton;
    public readonly Button DenyButton;
    public readonly Button ExtraButton;
    private readonly Label _label;

    public ConfirmationWindow()
    {
        Title = "";

        Contents.AddChild(new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Vertical,
            Children =
            {
                (_label = new Label()),
                new BoxContainer
                {
                    Orientation = BoxContainer.LayoutOrientation.Horizontal,
                    Align = BoxContainer.AlignMode.Center,
                    Children =
                    {
                        (AcceptButton = new Button()),
                        new Control { MinSize = new Vector2(20, 0) },
                        (ExtraButton = new Button { Visible = false }),
                        new Control { MinSize = new Vector2(20, 0) },
                        (DenyButton = new Button())
                    },
                },
            },
        });
    }

    public void Setup(string title, string text, string accept, string deny, string? extra = null)
    {
        Title = title;
        _label.Text = text;
        AcceptButton.Text = accept;
        DenyButton.Text = deny;

        if (extra != null)
        {
            ExtraButton.Text = extra;
            ExtraButton.Visible = true;
        }
        else
            ExtraButton.Visible = false;
    }
}

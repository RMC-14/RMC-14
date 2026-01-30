using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Localization;
using static Robust.Client.UserInterface.Controls.BoxContainer;

namespace Content.Client._RMC14.Marines.Mutiny;

public sealed class MutineerInviteWindow : DefaultWindow
{
    public Button DenyButton { get; }
    public Button AcceptButton { get; }

    public MutineerInviteWindow()
    {
        Title = Loc.GetString("mutineer-invite-title");

        AcceptButton = new Button
        {
            Text = Loc.GetString("mutineer-invite-accept")
        };

        DenyButton = new Button
        {
            Text = Loc.GetString("mutineer-invite-deny")
        };

        var layout = new BoxContainer
        {
            Orientation = LayoutOrientation.Vertical,
        };

        layout.AddChild(new RichTextLabel()
        {
            Text = "You are being asked to join a mutiny.",
            VerticalExpand =  true,
            VerticalAlignment = VAlignment.Center,
        });
        layout.AddChild(new RichTextLabel()
        {
            Text = "Read the Mutinies and Riots guidelines (Core Rules -> \"Mutinies, Riots\").",
            VerticalExpand =  true,
            VerticalAlignment = VAlignment.Center,
        });

        var buttonRow = new BoxContainer
        {
            Orientation = LayoutOrientation.Horizontal,
            Align = AlignMode.Center
        };

        buttonRow.AddChild(AcceptButton);
        buttonRow.AddChild(new Control
        {
            MinSize = new Vector2(20, 0)
        });
        buttonRow.AddChild(DenyButton);

        layout.AddChild(buttonRow);
        Contents.AddChild(layout);
    }
}

using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._RMC14.Requisitions;

public sealed partial class RequisitionsMessage : RichTextLabel
{
    public RequisitionsMessage()
    {
        RobustXamlLoader.Load(this);
    }
}

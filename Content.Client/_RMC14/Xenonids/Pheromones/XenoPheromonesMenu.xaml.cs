using Content.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._RMC14.Xenonids.Pheromones;

public sealed partial class XenoPheromonesMenu : RadialMenu
{
    public XenoPheromonesMenu()
    {
        RobustXamlLoader.Load(this);
    }
}

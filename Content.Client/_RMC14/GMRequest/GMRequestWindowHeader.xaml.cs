using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.XAML;

namespace Content.Client._RMC14.GMRequest;

/// <summary>
/// Header for the list of GMRequest Logs
/// </summary>
/// <remarks>
/// This exists as a separate container solely to prevent the header text from taking up 80% of the GMRequestWindow xaml
/// </remarks>
public sealed class GMRequestWindowHeader : BoxContainer
{
    public GMRequestWindowHeader()
    {
        RobustXamlLoader.Load(this);
    }
}

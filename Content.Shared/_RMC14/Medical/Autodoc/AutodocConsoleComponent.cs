using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Medical.Autodoc;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAutodocSystem))]
public sealed partial class AutodocConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? LinkedAutodoc;

    [DataField]
    public TimeSpan UpdateAt;

    [DataField]
    public TimeSpan UpdateCooldown = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public HashSet<AutodocUpgradeTier> InstalledUpgrades = [];
}

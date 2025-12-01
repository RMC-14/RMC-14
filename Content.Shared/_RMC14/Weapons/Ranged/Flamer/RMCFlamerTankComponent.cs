using Content.Shared.Whitelist;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Weapons.Ranged.Flamer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedRMCFlamerSystem))]
public sealed partial class RMCFlamerTankComponent : Component
{
    [DataField, AutoNetworkedField]
    public string SolutionId = "rmc_flamer_tank";

    [DataField, AutoNetworkedField]
    public EntityWhitelist? RefillWhitelist;

    [DataField, AutoNetworkedField]
    public int MaxIntensity = 20;

    [DataField, AutoNetworkedField]
    public int MaxDuration = 24;

    [DataField, AutoNetworkedField]
    public int MaxRange = 5;
}

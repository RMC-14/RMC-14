using Content.Shared.Chemistry.Reagent;
using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

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
    public int MinIntensity = 1;

    [DataField, AutoNetworkedField]
    public int MaxIntensity = 40;

    [DataField, AutoNetworkedField]
    public int MinDuration = 3;

    [DataField, AutoNetworkedField]
    public int MaxDuration = 30;

    [DataField, AutoNetworkedField]
    public int MaxRange = 5;

    [DataField, AutoNetworkedField]
    public string ExamineIcon = "/Textures/_RMC14/Structures/Storage/reagent_tank.rsi/weldtank.png";

    [DataField, AutoNetworkedField]
    public List<ProtoId<ReagentPrototype>>? ReagentWhitelist = null;

    [DataField, AutoNetworkedField]
    public bool Custom;

    [DataField, AutoNetworkedField]
    public bool Smoke;

    [DataField, AutoNetworkedField]
    public bool Specialist;

    [DataField, AutoNetworkedField]
    public bool AdjustablePressure;

    [DataField, AutoNetworkedField]
    public int FuelPressure = 1;

    [DataField, AutoNetworkedField]
    public int MaxPressure = 10;
}

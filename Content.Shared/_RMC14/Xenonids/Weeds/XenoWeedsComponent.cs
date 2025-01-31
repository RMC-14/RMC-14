using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Xenonids.Weeds;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoWeedsSystem))]
public sealed partial class XenoWeedsComponent : Component
{
    [DataField]
    public int Range = 5;

    [DataField]
    public float SpeedMultiplierXeno = 1.05f; //MOVE_DELAY * 0.95

    [DataField]
    public float SpeedMultiplierOutsider = 0.5714f;

    [DataField]
    public float SpeedMultiplierOutsiderArmor = 0.6666f;

    [DataField, AutoNetworkedField]
    public bool IsSource = true;

    [DataField, AutoNetworkedField]
    public EntityUid? Source;

    [DataField]
    public EntProtoId Spawns = "XenoWeeds";

    [DataField, AutoNetworkedField]
    public List<EntityUid> Spread = new();

    [DataField, AutoNetworkedField]
    public TimeSpan MinRandomDelete = TimeSpan.FromSeconds(9);

    [DataField, AutoNetworkedField]
    public TimeSpan MaxRandomDelete = TimeSpan.FromSeconds(10);

    [DataField, AutoNetworkedField]
    public bool SpreadsOnSemiWeedable = false;

    [DataField, AutoNetworkedField]
    public float FruitGrowthMultiplier = 1.0f;

    [DataField, AutoNetworkedField]
    public int Level = 1;

    [DataField, AutoNetworkedField]
    public bool BlockOtherWeeds;
}

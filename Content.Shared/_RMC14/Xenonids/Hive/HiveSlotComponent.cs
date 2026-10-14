using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Hive;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class HiveSlotComponent : Component
{
    [DataField, AutoNetworkedField]
    public int Position;
}

public static class HiveSlots
{
    public const int Normal = 1;
    public const int Corrupted = 2;
    public const int Alpha = 3;
    public const int Bravo = 4;
    public const int Charlie = 5;
    public const int Delta = 6;
    public const int Renegade = 7;
    public const int Forsaken = 8;
}

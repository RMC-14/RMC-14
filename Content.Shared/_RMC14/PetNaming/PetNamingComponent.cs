using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.PetNaming;

[RegisterComponent]
public sealed partial class PetNamingTargetComponent : Component
{
}

[RegisterComponent]
public sealed partial class PetNamingItemComponent : Component
{
    [DataField]
    public int MaxLength = 32;

    [DataField]
    public bool ConsumeOnUse = true;

    [DataField]
    public float Range = 1.5f;

    [ViewVariables]
    public EntityUid? Target;
}

[Serializable, NetSerializable]
public enum PetNamingUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class PetNamingBuiState : BoundUserInterfaceState
{
    public readonly string CurrentName;
    public readonly int MaxLength;
    public PetNamingBuiState(string currentName, int maxLength)
    {
        CurrentName = currentName;
        MaxLength = maxLength;
    }
}

[Serializable, NetSerializable]
public sealed class PetNamingSetNameBuiMsg : BoundUserInterfaceMessage
{
    public readonly string Name;
    public PetNamingSetNameBuiMsg(string name)
    {
        Name = name;
    }
}
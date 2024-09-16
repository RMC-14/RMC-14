using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class ParasiteAIComponent : Component
{
    [DataField, AutoNetworkedField]
    public ParasiteMode Mode = ParasiteMode.Active;

    [DataField]
    public TimeSpan NextActiveTime;

    [DataField]
    public TimeSpan? DeathTime;

    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromSeconds(30);

    [DataField]
    public int InitialJumps = 2;

    [DataField]
    public int JumpsLeft = 2;

    [DataField]
    public int MaxSurroundingParas = 2;
}

[Serializable, NetSerializable]
public enum ParasiteMode : byte
{
    Idle,
    Active,
    Dying
}

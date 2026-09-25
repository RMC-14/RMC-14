using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Xenonids.Parasite;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedXenoParasiteSystem))]
public sealed partial class ParasiteAIComponent : Component
{
    [DataField, AutoNetworkedField]
    public ParasiteMode Mode = ParasiteMode.Idle;

    [DataField]
    public TimeSpan NextActiveTime;

    [DataField]
    public TimeSpan? DeathTime;

    [DataField]
    public TimeSpan LifeTime = TimeSpan.FromSeconds(30);

    [DataField]
    public int InitialJumps = 5;

    [DataField]
    public int JumpsLeft = 5;

    [DataField]
    public int MaxSurroundingParas = 2;

    [DataField]
    public float MaxInfectRange = 1.5f;

    [DataField]
    public float IdleChance = 0.15f;

    [DataField]
    public float MinIdleTime = 4;

    [DataField]
    public float MaxIdleTime = 4;

    [DataField]
    public string RestAction = "ActionXenoRest";

    [DataField]
    public float RangeCheck = 1.5f;

    [DataField]
    public float CannibalizeCheck = 0.5f;

    [DataField]
    public TimeSpan JumpTime = TimeSpan.FromSeconds(2.5);

    [DataField]
    public TimeSpan? NextJump;
}

[Serializable, NetSerializable]
public enum ParasiteMode : byte
{
    Idle,
    Active,
    Dying
}

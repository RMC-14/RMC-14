using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Projectiles.Reflect;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCReflectedProjectileComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Accuracy = 35;

    [DataField, AutoNetworkedField]
    public float ReflectionMultiplier = 0.5f;

    [DataField, AutoNetworkedField]
    public HashSet<int> ReflectedBy = new ();

    [DataField, AutoNetworkedField]
    public int LastReflectedBy;
}

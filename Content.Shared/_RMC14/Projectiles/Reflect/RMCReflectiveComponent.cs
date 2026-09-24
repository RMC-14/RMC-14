using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Projectiles.Reflect;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class RMCReflectiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public Angle Angle = Angle.FromDegrees(60);

    [DataField, AutoNetworkedField]
    public float Chance = 0.75f;

    [DataField, AutoNetworkedField]
    public float Range = 10;

    [DataField, AutoNetworkedField]
    public float Accuracy = 35;

    [DataField, AutoNetworkedField]
    public float ReflectionMultiplier = 0.5f;
}

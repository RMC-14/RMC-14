namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCBunnyComponent : Component
{
    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public float HeardEmoteChance = 0.01f;

    [DataField]
    public float SeenEmoteChance = 0.01f;

    [DataField]
    public float ShooKnockback = 0.45f;

    [DataField]
    public float ShooKnockbackSpeed = 4f;

    [DataField]
    public TimeSpan KickPopupCooldown = TimeSpan.FromSeconds(2);

    [ViewVariables]
    public TimeSpan NextThinkAt;

    [ViewVariables]
    public TimeSpan NextKickPopupAt;
}

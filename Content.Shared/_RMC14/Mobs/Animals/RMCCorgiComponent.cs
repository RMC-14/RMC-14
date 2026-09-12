namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCCorgiComponent : Component
{
    [DataField]
    public bool IsPuppy;

    [DataField]
    public TimeSpan ThinkCooldown = TimeSpan.FromSeconds(4);

    [DataField]
    public float HeardEmoteChance = 0.04f;

    [DataField]
    public float SeenEmoteChance = 0.025f;

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

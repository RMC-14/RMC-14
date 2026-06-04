namespace Content.Shared._RMC14.Mobs.Animals;

[RegisterComponent]
public sealed partial class RMCLisaCorgiComponent : Component
{
    [DataField]
    public TimeSpan DanceCooldown = TimeSpan.FromSeconds(12);

    [DataField]
    public float DanceChance = 0.05f;

    [ViewVariables]
    public TimeSpan NextDanceAt;
}

using Robust.Shared.GameObjects;

namespace Content.Server._RMC14.Xenonids.AdrenalineSurge;

[RegisterComponent]
public sealed partial class XenoAdrenalineSurgeComponent : Component
{

    [DataField]
    public float SpeedModifierAmount = 1.65f;

    [DataField]
    public TimeSpan SurgeDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan Cooldown = TimeSpan.FromSeconds(20);

    public TimeSpan? SurgeEndTime;

    public bool IsSurging;

    public bool IsUsable = true;

    public bool ReadyMessageSent = false;
}

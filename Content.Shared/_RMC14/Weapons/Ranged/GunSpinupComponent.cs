using Robust.Shared.Audio;

namespace Content.Shared._RMC14.Weapons.Ranged;

[RegisterComponent]
[Access(typeof(GunSpinupSystem))]
public sealed partial class GunSpinupComponent : Component
{
    [DataField]
    public float BaseShotDelay = 0.7f;

    [DataField]
    public float BaseScatter = 18f;

    [DataField]
    public float SpinUpTime = 10f;

    [DataField]
    public float GraceAfterStop = 2f;

    [DataField]
    public float SpinDownTime = 3f;

    [DataField]
    public float MinSpinLevel = 1f;

    [DataField]
    public float MaxSpinLevel = 11f;

    [DataField]
    public int[] RateTiers = [1, 1, 2, 2, 3, 3, 3, 4, 4, 4, 5];

    [DataField]
    public SoundSpecifier? StartSound = new SoundPathSpecifier("/Audio/_RMC14/Vehicle/weapons/minigun_start.ogg");

    [DataField]
    public SoundSpecifier? LoopSound = new SoundPathSpecifier("/Audio/Weapons/Guns/Gunshots/minigun.ogg");

    [DataField]
    public SoundSpecifier? StopSound = new SoundPathSpecifier("/Audio/_RMC14/Vehicle/weapons/minigun_stop.ogg");

    [DataField]
    public SoundSpecifier? SelectSound = new SoundPathSpecifier("/Audio/_RMC14/Vehicle/weapons/minigun_select.ogg");

    [DataField]
    public float LoopSoundCooldown = 0.2f;

    [DataField]
    public float FireWindowPadding = 0.12f;

    [DataField]
    public float InitialWindupDelay = 0f;

    [DataField]
    public float InitialWindupResetGap = 0.2f;

    public TimeSpan LastUpdate;

    public TimeSpan? LastShotAt;

    public TimeSpan? LastAttemptAt;

    public TimeSpan? PendingWindupUntil;

    public TimeSpan? LastLoopSoundAt;

    public float CurrentSpinLevel = 1f;

    public float LastAppliedRate = -1f;

    public float LastAppliedScatter = -1f;

    public bool WasFiring;

    public bool StartSoundPlayed;
}

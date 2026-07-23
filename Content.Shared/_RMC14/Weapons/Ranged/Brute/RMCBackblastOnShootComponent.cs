using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared._RMC14.Weapons.Ranged.Brute;

[RegisterComponent]
public sealed partial class RMCBackblastOnShootComponent : Component
{
    // Backblast numbers for the tile directly behind the launcher.
    [DataField]
    public DamageSpecifier Damage = new()
    {
        DamageDict =
        {
            ["Blunt"] = 15,
        },
    };

    [DataField]
    public TimeSpan KnockdownTime = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan StutterTime = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan DeafTime = TimeSpan.FromSeconds(10);

    [DataField]
    public float TileRange = 0.45f;

    [DataField]
    public EntProtoId SmokePrototype = "RMCBruteSmoke";

    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_RMC14/Weapons/Guns/Gunshots/gun_rocketlauncher.ogg");
}

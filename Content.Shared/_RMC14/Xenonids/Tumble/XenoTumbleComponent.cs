using System.Numerics;
using Content.Shared.Damage;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Tumble;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(XenoTumbleSystem))]
public sealed partial class XenoTumbleComponent : Component
{
    [DataField, AutoNetworkedField]
    public float Range = 2;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoTailSwipe");

    [DataField, AutoNetworkedField]
    public Vector2? Target;

    [DataField, AutoNetworkedField]
    public TimeSpan StunTime = TimeSpan.FromSeconds(2);

    [DataField, AutoNetworkedField]
    public float ImpactRange = 1;

    [DataField, AutoNetworkedField]
    public DamageSpecifier Damage = new ();

    [DataField, AutoNetworkedField]
    public int ArmorPiercing = 100;
}

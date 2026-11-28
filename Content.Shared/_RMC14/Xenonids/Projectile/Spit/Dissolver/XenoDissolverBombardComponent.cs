using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Xenonids.Projectile.Spit.Dissolver;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoDissolverBombardComponent : Component
{
    [DataField, AutoNetworkedField]
    public int StoredAcid = 0;

    [DataField, AutoNetworkedField]
    public int MaxStoredAcid = 5;

    [DataField, AutoNetworkedField]
    public int ArmorReductionPerStoredAcid = 5;

    [DataField]
    public DoAfterId? BombardDoafter;

    [DataField, AutoNetworkedField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("XenoSpitAcid", AudioParams.Default.WithVolume(-10f));

    [DataField, AutoNetworkedField]
    public EntityCoordinates? TargetCoordinates = null;

    [DataField, AutoNetworkedField]
    public EntityUid? TargetEnt = null;

    [DataField, AutoNetworkedField]
    public TimeSpan BaseCooldown = TimeSpan.FromSeconds(11);

    [DataField, AutoNetworkedField]
    public TimeSpan ReducedCooldown = TimeSpan.FromSeconds(1);

    [DataField, AutoNetworkedField]
    public TimeSpan StoredAcidCooldown = TimeSpan.FromSeconds(4);

    [DataField, AutoNetworkedField]
    public ResPath AcidIconResPath = new ResPath("/Textures/_RMC14/Actions/xeno_actions.rsi");

    [DataField, AutoNetworkedField]
    public string AcidIconStatePrefix = "store_acid_";

}

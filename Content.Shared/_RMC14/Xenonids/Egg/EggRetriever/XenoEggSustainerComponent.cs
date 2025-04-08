using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared._RMC14.Xenonids.Egg.EggRetriever;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class XenoEggSustainerComponent : Component
{
    [DataField]
    public List<EntityUid> SustainedEggs = new();

    [DataField, AutoNetworkedField]
    public int MaxSustainedEggs = 4;

    [DataField, AutoNetworkedField]
    public int SustainedEggsRange = 14;

    [DataField, AutoNetworkedField]
    public TimeSpan SustainedEggMaxTime = TimeSpan.FromMinutes(5);

    [DataField, AutoNetworkedField]
    public SoundSpecifier DeathSound = new SoundPathSpecifier("/Audio/_RMC14/Xeno/alien_egg_burst.ogg");
}

using Content.Shared.NPC.Prototypes;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._RMC14.Marines;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedMarineSystem))]
public sealed partial class MarineComponent : Component
{
    [DataField, AutoNetworkedField]
    public SpriteSpecifier? Icon;

    [DataField, AutoNetworkedField]
    public Dictionary<ProtoId<NpcFactionPrototype>, SpriteSpecifier> GenericFactionIcons = new()
    {
        { "UNMC", new SpriteSpecifier.Rsi(new("/Textures/_RMC14/Interface/faction_icons.rsi"), "unmc") },
        { "SPP", new SpriteSpecifier.Rsi(new ("/Textures/_RMC14/Interface/faction_icons.rsi"), "spp") },
        { "WeYa", new SpriteSpecifier.Rsi(new ("/Textures/_RMC14/Interface/faction_icons.rsi"), "weya") },
        { "RoyalMarines", new SpriteSpecifier.Rsi(new("/Textures/_RMC14/Interface/faction_icons.rsi"), "tse") },
        { "TSE", new SpriteSpecifier.Rsi(new("/Textures/_RMC14/Interface/faction_icons.rsi"), "tse") },
        { "CLF", new SpriteSpecifier.Rsi(new("/Textures/_RMC14/Interface/faction_icons.rsi"), "clf") },
    };
}

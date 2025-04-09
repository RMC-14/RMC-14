using Content.Client._RMC14.Xenonids.Construction;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Sprite;
using Robust.Client.GameObjects;
using Robust.Client.ResourceManagement;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
using System.Linq;

namespace Content.Client._RMC14.Sprite;

public sealed class RMCAltSpriteVisualizerSystem : VisualizerSystem<RMCAlternateSpriteComponent>
{

    [Dependency] private readonly IConfigurationManager _configuration = default!;
    [Dependency] private readonly IResourceCache _resourceCache = default!;

    private bool _useAlternateSprites;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCAlternateSpriteComponent, ComponentStartup>(OnInit);

        Subs.CVar(_configuration, RMCCVars.RMCUseAlternateSprites, OnAlternateSpriteChange, true);
    }

    private void OnAlternateSpriteChange(bool value, in CVarChangeInfo info)
    {
        _useAlternateSprites = value;

        var query = EntityQueryEnumerator<RMCAlternateSpriteComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            ChangeSprite((uid, comp));
        }
    }

    private void OnInit(Entity<RMCAlternateSpriteComponent> ent, ref ComponentStartup args)
    {
        ChangeSprite((ent, ent.Comp));
    }

    protected override void OnAppearanceChange(EntityUid uid, RMCAlternateSpriteComponent component, ref AppearanceChangeEvent args)
    {
        base.OnAppearanceChange(uid, component, ref args);

        ChangeSprite((uid, component));
    }

    private void ChangeSprite(Entity<RMCAlternateSpriteComponent> ent)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

        var newSprite = _useAlternateSprites ? ent.Comp.AlternateSprite : ent.Comp.NormalSprite;

        if (!_resourceCache.TryGetResource<RSIResource>(SpriteSpecifierSerializer.TextureRoot / newSprite, out var res))
        {
            Log.Error("Unable to load RSI '{0}'. Trace:\n{1}", newSprite, Environment.StackTrace);
            return;
        }

        if (sprite.BaseRSI != res.RSI)
        {
            sprite.BaseRSI = res.RSI;

            //Reset frames
            for (var i = 0; i < sprite.AllLayers.Count(); i++)
            {
                sprite.LayerSetAnimationTime(i, 0);
            }
        }
    }
}

using System.Numerics;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.NewPlayer;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Player;

namespace Content.Client._RMC14.NewPlayer;

public sealed class NewPlayerVisualizerSystem : VisualizerSystem<NewPlayerLabelComponent>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _configManager = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<SeeNewPlayersComponent> _seeNewPlayersQuery;
    private bool _showPlayerIcons;

    public override void Initialize()
    {
        base.Initialize();

        _seeNewPlayersQuery = GetEntityQuery<SeeNewPlayersComponent>();

        SubscribeLocalEvent<SeeNewPlayersComponent, LocalPlayerAttachedEvent>(OnSeeNewPlayersLocalAttached);
        SubscribeLocalEvent<SeeNewPlayersComponent, LocalPlayerDetachedEvent>(OnSeeNewPlayersLocalDetached);

        Subs.CVar(_configManager, RMCCVars.RMCShowNewPlayerIcons, NewPlayerIconsOptionChanged, true);
    }

    private void OnSeeNewPlayersLocalAttached(Entity<SeeNewPlayersComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        UpdateAllAppearance();
    }

    private void OnSeeNewPlayersLocalDetached(Entity<SeeNewPlayersComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        UpdateAllAppearance();
    }

    protected override void OnAppearanceChange(EntityUid uid, NewPlayerLabelComponent component, ref AppearanceChangeEvent args)
    {
        if (args.Sprite == null)
            return;

        UpdateAppearance((uid, args.Component, args.Sprite));
    }

    private void UpdateAllAppearance()
    {
        var query = AllEntityQuery<NewPlayerLabelComponent, AppearanceComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out var appearance, out var sprite))
        {
            UpdateAppearance((uid, appearance, sprite));
        }
    }

    private void UpdateAppearance(Entity<AppearanceComponent, SpriteComponent> ent)
    {
        var spriteEntity = (ent.Owner, ent.Comp2);

        if (!_sprite.LayerMapTryGet(spriteEntity, NewPlayerLayers.Layer, out var layer, false))
            return;

        if (!_seeNewPlayersQuery.TryComp(_player.LocalEntity, out var see) ||
            !AppearanceSystem.TryGetData(ent, NewPlayerLayers.Layer, out NewPlayerVisuals visual, ent) ||
            !_showPlayerIcons)
        {
            _sprite.LayerSetVisible(spriteEntity, layer, false);
            return;
        }

        var state = visual switch
        {
            NewPlayerVisuals.One => see.OneLabel,
            NewPlayerVisuals.Two => see.TwoLabel,
            NewPlayerVisuals.Three => see.ThreeLabel,
            NewPlayerVisuals.Four => see.FourLabel,
            _ => null,
        };

        if (state == null)
            return;

        _sprite.LayerSetSprite(spriteEntity, layer, state);
        _sprite.LayerSetVisible(spriteEntity, layer, true);
        _sprite.LayerSetOffset(spriteEntity, layer, new Vector2(0, 0.21f));
    }

    private void NewPlayerIconsOptionChanged(bool enabled)
    {
        _showPlayerIcons = enabled;
        UpdateAllAppearance();
    }
}

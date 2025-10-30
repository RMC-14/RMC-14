using System.Linq;
using Content.Shared._RMC14.BuriedItems;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.GameObjects;

namespace Content.Client._RMC14.BuriedItems;

/// <summary>
/// Client-side system that controls buried item sprite visibility based on whether
/// the local player has the SeeBuriedItemsComponent (admin ghosts only).
/// </summary>
public sealed class BuriedItemsVisualsSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BuriedItemsComponent, ComponentStartup>(OnBuriedItemsStartup);
        SubscribeLocalEvent<SeeBuriedItemsComponent, ComponentStartup>(OnSeeBuriedStartup);
        SubscribeLocalEvent<SeeBuriedItemsComponent, ComponentShutdown>(OnSeeBuriedShutdown);

        // Listen for when the local player entity changes (e.g., aghost -> body)
        _player.LocalPlayerAttached += OnLocalPlayerAttached;
        _player.LocalPlayerDetached += OnLocalPlayerDetached;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _player.LocalPlayerAttached -= OnLocalPlayerAttached;
        _player.LocalPlayerDetached -= OnLocalPlayerDetached;
    }

    private void OnLocalPlayerAttached(EntityUid uid)
    {
        // When we attach to a new entity (e.g., returning from aghost to body), update all buried items
        UpdateAllBuriedItems();
    }

    private void OnLocalPlayerDetached(EntityUid uid)
    {
        // When we detach from an entity, update all buried items
        UpdateAllBuriedItems();
    }

    private void OnBuriedItemsStartup(EntityUid uid, BuriedItemsComponent component, ComponentStartup args)
    {
        UpdateVisibility(uid);
    }

    private void OnSeeBuriedStartup(EntityUid uid, SeeBuriedItemsComponent component, ComponentStartup args)
    {
        // When the local player gains the ability to see buried items, update all buried items
        if (_player.LocalEntity == uid)
            UpdateAllBuriedItems();
    }

    private void OnSeeBuriedShutdown(EntityUid uid, SeeBuriedItemsComponent component, ComponentShutdown args)
    {
        // When the local player loses the ability to see buried items, update all buried items
        if (_player.LocalEntity == uid)
            UpdateAllBuriedItems();
    }

    private void UpdateAllBuriedItems()
    {
        var query = EntityQueryEnumerator<BuriedItemsComponent, SpriteComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            UpdateVisibility(uid);
        }
    }

    private void UpdateVisibility(EntityUid uid)
    {
        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        var localPlayer = _player.LocalEntity;
        if (localPlayer == null)
            return;

        // Check if the local player can see buried items
        var canSee = HasComp<SeeBuriedItemsComponent>(localPlayer.Value);

        // Update the sprite layer color to make it visible or invisible
        if (sprite.AllLayers.Any())
        {
            var layer = 0; // First layer is the buried item sprite

            // Set alpha to full (FF) if can see, or 0 if can't see
            var newColor = canSee
                ? Color.White
                : Color.White.WithAlpha(0.0f);

            _sprite.LayerSetColor((uid, sprite), layer, newColor);
        }
    }
}

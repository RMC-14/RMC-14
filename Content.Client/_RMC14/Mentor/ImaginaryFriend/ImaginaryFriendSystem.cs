using Content.Client.Ghost;
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Mentor.ImaginaryFriend;

public sealed partial class ImaginaryFriendSystem : SharedImaginaryFriendSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ImaginaryFriendComponent, ComponentStartup>(OnStartup, after: [typeof(GhostSystem)]);
        SubscribeLocalEvent<ImaginaryFriendComponent, AppearanceChangeEvent>(OnAppearanceChange);
    }

    private void OnStartup(Entity<ImaginaryFriendComponent> ent, ref ComponentStartup args)
    {
        UpdateAppearance(ent);
    }

    private void OnAppearanceChange(Entity<ImaginaryFriendComponent> ent, ref AppearanceChangeEvent args)
    {
        UpdateAppearance(ent);
    }

    private void UpdateAppearance(Entity<ImaginaryFriendComponent> ent)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        var spriteEnt = (ent, sprite);
        var clientIsFriend = _player.LocalEntity == ent;
        var clientHasFriend = _player.LocalEntity == ent.Comp.Imaginer;

        var alpha = 1f;
        if (!ent.Comp.Visible)
        {
            if (clientIsFriend)
                alpha = 0.5f;

            if (clientHasFriend)
                alpha = 0;
        }

        _sprite.SetColor(spriteEnt, Color.White.WithAlpha(alpha));
        _sprite.SetVisible(spriteEnt, clientIsFriend || clientHasFriend);
    }
}

using Content.Client.Ghost;
using Content.Shared._RMC14.Mentor.ImaginaryFriend;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Player;

namespace Content.Client._RMC14.Mentor.ImaginaryFriend;

public sealed partial class ImaginaryFriendSystem : SharedImaginaryFriendSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private const float AlphaWhileHidden = 0;

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
        var localEntity = _player.LocalEntity;
        var clientIsFriend = localEntity== ent;
        var clientHasFriend = localEntity == ent.Comp.Imaginer;

        var alpha = ent.Comp.DefaultAlpha;
        if (!ent.Comp.Visible)
        {
            if (clientIsFriend)
                alpha = ent.Comp.OwnAlphaWhileHidden;

            if (clientHasFriend)
                alpha = AlphaWhileHidden;
        }

        _sprite.SetColor(spriteEnt, Color.White.WithAlpha(alpha));
        _sprite.SetVisible(spriteEnt, clientIsFriend || clientHasFriend || HasComp<GhostComponent>(localEntity));
    }
}

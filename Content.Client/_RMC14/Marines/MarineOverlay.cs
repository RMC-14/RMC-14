using System.Linq;
using System.Numerics;
using Content.Shared._RMC14.Marines;
using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Tracker.SquadLeader;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Marines;

public sealed class MarineOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private static readonly SpriteSpecifier.Rsi FireteamOneRsi = new(new ResPath("_RMC14/Interface/marine_hud.rsi"), "hudsquad_ft1");
    private static readonly SpriteSpecifier.Rsi FireteamTwoRsi = new(new ResPath("_RMC14/Interface/marine_hud.rsi"), "hudsquad_ft2");
    private static readonly SpriteSpecifier.Rsi FireteamThreeRsi = new(new ResPath("_RMC14/Interface/marine_hud.rsi"), "hudsquad_ft3");
    private static readonly SpriteSpecifier.Rsi FireteamLeaderRsi = new(new ResPath("_RMC14/Interface/marine_hud.rsi"), "hudsquad_ftl");

    private readonly NpcFactionSystem _npcFaction;
    private readonly ContainerSystem _container;
    private readonly MarineSystem _marine;
    private readonly SpriteSystem _sprite;
    private readonly TransformSystem _transform;

    private readonly ShaderInstance _shader;

    private readonly EntityQuery<NpcFactionMemberComponent> _npcFactionMemberQuery;
    private readonly EntityQuery<FireteamLeaderComponent> _fireteamLeaderQuery;
    private readonly EntityQuery<FireteamMemberComponent> _fireteamMemberQuery;
    private readonly EntityQuery<EntityActiveInvisibleComponent> _invisQuery;
    private readonly EntityQuery<ShowMarineIconsComponent> _marineIconsQuery;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowFOV;

    public MarineOverlay()
    {
        IoCManager.InjectDependencies(this);

        _npcFaction = _entity.System<NpcFactionSystem>();
        _container = _entity.System<ContainerSystem>();
        _marine = _entity.System<MarineSystem>();
        _sprite = _entity.System<SpriteSystem>();
        _transform = _entity.System<TransformSystem>();

        _npcFactionMemberQuery = _entity.GetEntityQuery<NpcFactionMemberComponent>();
        _fireteamLeaderQuery = _entity.GetEntityQuery<FireteamLeaderComponent>();
        _fireteamMemberQuery = _entity.GetEntityQuery<FireteamMemberComponent>();
        _invisQuery = _entity.GetEntityQuery<EntityActiveInvisibleComponent>();
        _marineIconsQuery = _entity.GetEntityQuery<ShowMarineIconsComponent>();

        _shader = _prototype.Index<ShaderPrototype>("shaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (!_marineIconsQuery.TryComp(_players.LocalEntity, out var marineHudComp))
            return;

        var handle = args.WorldHandle;

        var eyeRot = args.Viewport.Eye?.Rotation ?? default;

        var xformQuery = _entity.GetEntityQuery<TransformComponent>();
        var scaleMatrix = Matrix3x2.CreateScale(new Vector2(1, 1));
        var rotationMatrix = Matrix3Helpers.CreateRotation(-eyeRot);

        handle.UseShader(_shader);

        var fireteamOneIcon = _sprite.Frame0(FireteamOneRsi);
        var fireteamTwoIcon = _sprite.Frame0(FireteamTwoRsi);
        var fireteamThreeIcon = _sprite.Frame0(FireteamThreeRsi);
        var fireteamLeaderIcon = _sprite.Frame0(FireteamLeaderRsi);

        var marineQuery = _entity.AllEntityQueryEnumerator<MarineComponent, StatusIconComponent, SpriteComponent, TransformComponent>();
        while (marineQuery.MoveNext(out var uid, out _, out var status, out var sprite, out var xform))
        {
            if (xform.MapID != args.MapId)
                continue;

            var bounds = status.Bounds ?? sprite.Bounds;

            var worldPos = _transform.GetWorldPosition(xform, xformQuery);

            if (!bounds.Translated(worldPos).Intersects(args.WorldAABB))
                continue;

            if (_container.IsEntityOrParentInContainer(uid)) // prevent icons being visible inside containers
                continue;

            if (_invisQuery.HasComp(uid))
                continue;

            var worldMatrix = Matrix3x2.CreateTranslation(worldPos);
            var scaledWorld = Matrix3x2.Multiply(scaleMatrix, worldMatrix);
            var matrix = Matrix3x2.Multiply(rotationMatrix, scaledWorld);
            handle.SetTransform(matrix);

            var icon = _marine.GetMarineIcon(uid);
            var factionIcons = _marine.GetFactionIcons(uid);

            if (marineHudComp.Factions != null && !_npcFaction.IsMemberOfAny(uid, marineHudComp.Factions) && factionIcons != null)
            {
                if (_npcFactionMemberQuery.TryComp(uid, out var factionMember))
                {
                    // First faction is the entity's default faction
                    if (factionIcons.TryGetValue(factionMember.Factions.First(), out var newIcon))
                    {
                        icon.Background = null;
                        icon.Icon = newIcon;
                    }
                }
            }

            if (icon.Icon != null)
            {
                var texture = _sprite.Frame0(icon.Icon);
                var yOffset = 0.1f + (bounds.Height + sprite.Offset.Y) / 2f - (float)texture.Height / EyeManager.PixelsPerMeter;
                var xOffset = 0.1f + (bounds.Width + sprite.Offset.X) / 2f - (float)texture.Width / EyeManager.PixelsPerMeter;
                var position = new Vector2(xOffset, yOffset);
                if (icon.Icon != null && icon.Background != null)
                {
                    var background = _sprite.Frame0(icon.Background);
                    handle.DrawTexture(background, position, icon.BackgroundColor);
                }

                handle.DrawTexture(texture, position);
            }

            if (_fireteamMemberQuery.TryComp(uid, out var member))
            {
                var texture = member.Fireteam switch
                {
                    0 => fireteamOneIcon,
                    1 => fireteamTwoIcon,
                    2 => fireteamThreeIcon,
                    _ => null,
                };

                if (texture != null)
                {
                    var offset = -(float)fireteamOneIcon.Height / 2 / EyeManager.PixelsPerMeter;
                    var yOffset = 0.1f + (bounds.Height + sprite.Offset.Y + offset) / 2f - (float)texture.Height / EyeManager.PixelsPerMeter;
                    var xOffset = (bounds.Width + sprite.Offset.X) / 2f - (float)texture.Width / EyeManager.PixelsPerMeter;
                    var position = new Vector2(xOffset, yOffset);
                    handle.DrawTexture(texture, position, icon.BackgroundColor);
                }
            }

            if (_fireteamLeaderQuery.HasComp(uid))
            {
                var texture = fireteamLeaderIcon;
                var offset = -(float)fireteamOneIcon.Height / 2 / EyeManager.PixelsPerMeter;
                var yOffset = 0.1f + (bounds.Height + sprite.Offset.Y + offset) / 2f - (float)texture.Height / EyeManager.PixelsPerMeter;
                var xOffset = (bounds.Width + sprite.Offset.X) / 2f - (float)texture.Width / EyeManager.PixelsPerMeter;
                var position = new Vector2(xOffset, yOffset);
                handle.DrawTexture(texture, position, icon.BackgroundColor);
            }
        }

        handle.UseShader(null);
    }
}

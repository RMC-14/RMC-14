using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.GhostAppearance;
using Content.Shared._RMC14.Xenonids;
using Content.Shared._RMC14.Xenonids.Damage;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.GhostAppearance;

public sealed class DeadGhostVisualsSystem : EntitySystem
{
    [Dependency] private readonly ActorSystem _actor = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly INetConfigurationManager _netConfigManager = default!;

    private readonly float _opacity = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCGhostAppearanceComponent, ComponentStartup>(OnStartup);
    }

    private void OnStartup(Entity<RMCGhostAppearanceComponent> ent, ref ComponentStartup args)
    {
        var session = _actor.GetSession(ent.Owner);

        if (session == null)
            return;

        if (!_netConfigManager.GetClientCVar(session.Channel, RMCCVars.RMCGhostAppearanceFromDeadCharacter))
            return;

        var attachedEntity = session.AttachedEntity;

        if (!TryComp(ent.Owner, out SpriteComponent? sprite))
            return;

        if (!TryComp(attachedEntity, out SpriteComponent? attachedSprite))
            return;

        sprite.CopyFrom(attachedSprite);
        sprite.Rotation = Angle.Zero;
        sprite.PostShader = _prototypes.Index<ShaderPrototype>("RMCInvisible").InstanceUnique();
        sprite.PostShader.SetParameter("visibility", _opacity);

        if (HasComp<XenoComponent>(attachedEntity)) // update xeno visuals
        {
            if (sprite is not { BaseRSI: { } rsi } ||
                !sprite.LayerMapTryGet(XenoVisualLayers.Base, out var layer))
            {
                return;
            }

            if (rsi.TryGetState("alive", out _))
                sprite.LayerSetState(layer, "alive");

            if (!sprite.LayerMapTryGet(RMCDamageVisualLayers.Base, out var damageLayer))
                return;

            sprite.LayerSetVisible(damageLayer, false); // set damage visuals invisible
        }
    }
}

using Content.Client._RMC14.Ghost;
using Content.Shared._RMC14.CCVar;
using Content.Shared._RMC14.Ghost;
using Content.Shared._RMC14.GhostColor;
using Content.Shared.Ghost;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.GhostColor;

public sealed class GhostColorSystem : EntitySystem
{
    private static readonly ProtoId<ShaderPrototype> GhostAlphaShader = "RMCGhostAlpha";

    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private float _observerAlpha;
    private ShaderInstance _alphaShader = default!;

    public override void Initialize()
    {
        _alphaShader = _prototype.Index(GhostAlphaShader).InstanceUnique();

        SubscribeLocalEvent<GhostColorComponent, AfterAutoHandleStateEvent>(OnGhostColorState);
        SubscribeLocalEvent<GhostColorComponent, ComponentShutdown>(OnGhostColorShutdown);
        SubscribeLocalEvent<GhostHumanoidAppearanceComponent, GhostAppearanceLayersRefreshedEvent>(OnLayersRefreshed);
        SubscribeLocalEvent<GhostNonHumanoidAppearanceComponent, GhostAppearanceLayersRefreshedEvent>(OnNonHumanoidLayersRefreshed);

        Subs.CVar(_config, RMCCVars.RMCGhostBodyAlpha, v => _alphaShader.SetParameter("opacity", v), true);
        Subs.CVar(_config, RMCCVars.RMCGhostObserverAlpha, v => _observerAlpha = v, true);
    }

    private void OnGhostColorState(Entity<GhostColorComponent> ent, ref AfterAutoHandleStateEvent _)
    {
        ApplyColor(ent);
    }

    private void OnGhostColorShutdown(Entity<GhostColorComponent> ent, ref ComponentShutdown _)
    {
        if (TryComp(ent, out GhostComponent? ghost))
            ApplyColor(ent, ghost.Color);
    }

    private void OnLayersRefreshed(Entity<GhostHumanoidAppearanceComponent> ent, ref GhostAppearanceLayersRefreshedEvent _)
    {
        ApplyColor(ent);
    }

    private void OnNonHumanoidLayersRefreshed(Entity<GhostNonHumanoidAppearanceComponent> ent, ref GhostAppearanceLayersRefreshedEvent _)
    {
        ApplyColor(ent);
    }

    public void ApplyColor(EntityUid uid, Color? colorOverride = null)
    {
        if (!TryComp(uid, out GhostComponent? ghost) || !TryComp(uid, out SpriteComponent? sprite))
            return;

        Entity<SpriteComponent?> spriteEnt = (uid, sprite);
        var ghostColor = colorOverride ?? CompOrNull<GhostColorComponent>(uid)?.Color ?? ghost.Color;

        if (!HasComp<GhostHumanoidAppearanceComponent>(uid) && !HasComp<GhostNonHumanoidAppearanceComponent>(uid))
        {
            if (sprite.PostShader == _alphaShader)
                sprite.PostShader = null;

            _sprite.SetColor(spriteEnt, ghostColor.WithAlpha(_observerAlpha));
            return;
        }

        _sprite.SetColor(spriteEnt, ghostColor.WithAlpha(1f));
        sprite.PostShader = _alphaShader;
    }
}

using System.Linq;
using Content.Shared._RMC14.Stealth;
using Content.Shared._RMC14.Xenonids.Invisibility;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Stealth;

public sealed class EntityInvisibilityVisualsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityTurnInvisibleComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<EntityTurnInvisibleComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<EntityTurnInvisibleComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        sprite.PostShader = _prototypes.Index<ShaderPrototype>("RMCInvisible").InstanceUnique();
    }

    private void OnShutdown(Entity<EntityTurnInvisibleComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        sprite.PostShader = null;
    }

    private void SetShaderParams(SpriteComponent sprite, EntityActiveInvisibleComponent? activeInvis)
    {
        var opacity = activeInvis?.Opacity ?? 1;
        var greyFactor = activeInvis?.GreyFactor ?? 0;
        var darkFactor = activeInvis?.DarkFactor ?? 0;

        sprite.PostShader?.SetParameter("opacity", opacity);
        sprite.PostShader?.SetParameter("grey_factor", greyFactor);
        sprite.PostShader?.SetParameter("dark_factor", darkFactor);
    }

    public override void Update(float frameTime)
    {
        var marineInvis = EntityQueryEnumerator<EntityTurnInvisibleComponent, SpriteComponent>();
        while (marineInvis.MoveNext(out var uid, out _, out var sprite))
        {
            SetShaderParams(sprite, CompOrNull<EntityActiveInvisibleComponent>(uid));
        }

        // TODO RMC14 having two TurnInvisible components for marine/xeno that share functionality
        // is a bit weird. Consider refactoring to remove some duplication
        var xenoInvis = EntityQueryEnumerator<XenoTurnInvisibleComponent, SpriteComponent>();
        while (xenoInvis.MoveNext(out var uid, out _, out var sprite))
        {
            // TODO RMC14 Xenos can't use the invis PostShader marines use, because the night vision overlay
            // does not work with post shaders. A future engine update (v286 I believe) should enable a fix.
            // At that point, GreyFactor can also be implemented for xenos.
            var activeInvis = CompOrNull<EntityActiveInvisibleComponent>(uid);
            var opacity = activeInvis?.Opacity ?? 1;
            var darkFactor = activeInvis?.DarkFactor ?? 0;

            var k = 1 - float.Clamp(darkFactor, 0, 1);
            sprite.Color = new Color(k, k, k, opacity);
        }
    }
}

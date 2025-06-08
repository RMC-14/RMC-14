using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Stealth;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Aura;

public sealed class AuraSystem : SharedAuraSystem
{
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AuraComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<AuraComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnStartup(Entity<AuraComponent> ent, ref ComponentStartup args)
    {
        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        sprite.PostShader = _prototypes.Index<ShaderPrototype>("RMCAuraOutline").InstanceUnique();
    }

    private void OnShutdown(Entity<AuraComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (HasComp<EntityActiveInvisibleComponent>(ent))
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        sprite.PostShader = null;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var auraQuery = EntityQueryEnumerator<AuraComponent, SpriteComponent>();

        while (auraQuery.MoveNext(out var uid, out var aura, out var sprite))
        {
            sprite.PostShader?.SetParameter("outline_color", aura.Color);
            sprite.PostShader?.SetParameter("outline_width", aura.OutlineWidth);
        }
    }
}

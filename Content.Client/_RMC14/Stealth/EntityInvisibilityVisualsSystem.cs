using Content.Shared._RMC14.Stealth;
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

        sprite.PostShader = _prototypes.Index<ShaderPrototype>("RMCInvisibile").InstanceUnique();
    }

    private void OnShutdown(Entity<EntityTurnInvisibleComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        sprite.PostShader = null;
    }

    public override void Update(float frameTime)
    {
        var invisible = EntityQueryEnumerator<EntityTurnInvisibleComponent, SpriteComponent>();
        while (invisible.MoveNext(out var uid, out var comp, out var sprite))
        {
            var opacity =  TryComp<EntityActiveInvisibleComponent>(uid, out var activeInvisible) ? activeInvisible.Opacity : 1;
            sprite.PostShader?.SetParameter("visibility", opacity);
        }
    }
}

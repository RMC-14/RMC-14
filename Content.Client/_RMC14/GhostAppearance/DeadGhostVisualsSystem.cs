using Content.Client.Smoking;
using Content.Shared._RMC14.GhostAppearance;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.GhostAppearance;

public sealed class DeadGhostVisualsSystem : SharedDeadGhostVisualsSystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private EntityQuery<AppearanceComponent> _appearanceQuery;

    private readonly float _opacity = 0.5f;

    public override void Initialize()
    {
        base.Initialize();

        _appearanceQuery = GetEntityQuery<AppearanceComponent>();

        SubscribeLocalEvent<RMCGhostAppearanceComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnHandleState(Entity<RMCGhostAppearanceComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        CopyComp<SpriteComponent>(ent);
        CopyComp<GenericVisualizerComponent>(ent);
        CopyComp<BurnStateVisualsComponent>(ent);

        // reload appearance to hopefully prevent any invisible layers
        if (_appearanceQuery.TryComp(ent, out var appearance))
            _appearance.QueueUpdate(ent, appearance);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var entities = EntityQueryEnumerator<RMCGhostAppearanceComponent, SpriteComponent>();
        while (entities.MoveNext(out _, out var sprite))
        {
            sprite.PostShader = _prototypes.Index<ShaderPrototype>("RMCInvisible").InstanceUnique();
            sprite.PostShader.SetParameter("visibility", _opacity);
        }
    }
}

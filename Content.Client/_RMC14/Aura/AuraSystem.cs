using Content.Client._RMC14.Graphics;
using Content.Client.Graphics;
using Content.Shared._RMC14.Aura;
using Content.Shared._RMC14.Stealth;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Prototypes;

namespace Content.Client._RMC14.Aura;

public sealed partial class AuraSystem : SharedAuraSystem
{
    private static readonly ProtoId<ShaderPrototype> AuraShader = "RMCAuraOutline";

    [Dependency] private IPrototypeManager _prototypes = default!;
    [Dependency] private SpriteSystem _sprite = default!;

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

        UpdateShader((ent, ent.Comp, sprite));
    }

    private void OnShutdown(Entity<AuraComponent> ent, ref ComponentShutdown args)
    {
        if (TerminatingOrDeleted(ent))
            return;

        if (!TryComp(ent, out SpriteComponent? sprite))
            return;

        _sprite.RemovePostShader((ent, sprite), RMCPostShaderIds.Aura);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var auraQuery = EntityQueryEnumerator<AuraComponent, SpriteComponent>();

        while (auraQuery.MoveNext(out var uid, out var aura, out var sprite))
        {
            UpdateShader((uid, aura, sprite));
        }
    }

    private void UpdateShader(Entity<AuraComponent, SpriteComponent> ent)
    {
        if (HasComp<EntityActiveInvisibleComponent>(ent))
        {
            _sprite.RemovePostShader((ent, ent.Comp2), RMCPostShaderIds.Aura);
            return;
        }

        ShaderInstance shader;
        if (_sprite.TryGetPostShader(ent.Comp2, RMCPostShaderIds.Aura, out var entry))
        {
            shader = entry.Shader;
        }
        else
        {
            shader = _prototypes.Index(AuraShader).InstanceUnique();
            _sprite.SetPostShader((ent, ent.Comp2), new SpriteComponent.PostShaderArgs(RMCPostShaderIds.Aura, shader)
            {
                After = ContentPostShaderIds.AfterBaseEffects,
                Before = ContentPostShaderIds.BeforeOutlines,
            });
        }

        shader.SetParameter("outline_color", ent.Comp1.Color);
        shader.SetParameter("outline_width", ent.Comp1.OutlineWidth);
    }
}

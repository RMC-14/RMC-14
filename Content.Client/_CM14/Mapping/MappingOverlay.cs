using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client._CM14.Mapping;

public sealed class MappingOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entities = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;

    private readonly MappingState _state;
    private readonly ShaderInstance _shader;

    public override OverlaySpace Space => OverlaySpace.WorldSpace;

    public MappingOverlay(MappingState state)
    {
        IoCManager.InjectDependencies(this);

        _state = state;
        _shader = _prototypes.Index<ShaderPrototype>("unshaded").Instance();
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_player.LocalEntity is not { } entity)
            return;

        var map = _entities.GetComponent<TransformComponent>(entity).MapID;
        if (map == MapId.Nullspace)
            return;

        var handle = args.WorldHandle;
        handle.UseShader(_shader);

        // TODO

        handle.UseShader(null);
    }
}

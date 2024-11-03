using Content.Shared._RMC14.Telephone;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Enums;
using Robust.Shared.Map;

namespace Content.Client._RMC14.Telephone;

public sealed class TelephoneOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _entity = default!;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    public TelephoneOverlay()
    {
        IoCManager.InjectDependencies(this);
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        var containerSystem = _entity.System<ContainerSystem>();
        var transformSystem = _entity.System<TransformSystem>();
        var handle = args.WorldHandle;

        var rotarys = _entity.EntityQueryEnumerator<RotaryPhoneComponent>();
        while (rotarys.MoveNext(out var rotaryId, out var rotary))
        {
            if (rotary.Phone is not { Valid: true } phone ||
                !containerSystem.TryGetContainer(rotaryId, rotary.ContainerId, out var container) ||
                container.ContainedEntities.Count > 0 ||
                !_entity.TryGetComponent(rotaryId, out TransformComponent? rotaryTransform) ||
                rotaryTransform.MapID == MapId.Nullspace ||
                !_entity.TryGetComponent(phone, out TransformComponent? phoneTransform) ||
                phoneTransform.MapID == MapId.Nullspace)
            {
                continue;
            }

            var rotaryPosition = transformSystem.GetMapCoordinates(rotaryId);
            var phonePosition = transformSystem.GetMapCoordinates(phone);
            handle.DrawLine(rotaryPosition.Position, phonePosition.Position, Color.Black);
        }
    }
}

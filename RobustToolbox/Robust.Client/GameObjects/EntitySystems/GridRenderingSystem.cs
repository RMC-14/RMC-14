using Robust.Client.Graphics;
using Robust.Shared.GameObjects;

namespace Robust.Client.GameObjects;

internal sealed class GridRenderingSystem : EntitySystem
{
    private readonly IClydeInternal _clyde;

    public GridRenderingSystem(IClydeInternal clyde)
    {
        _clyde = clyde;
    }

    public override void Initialize()
    {
        _clyde.RegisterGridEcsEvents();
    }

    public override void Shutdown()
    {
        _clyde.ShutdownGridEcsEvents();
    }
}

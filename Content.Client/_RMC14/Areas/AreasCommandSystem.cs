using Content.Shared._RMC14.Areas;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Areas;

public sealed class AreasCommandSystem : EntitySystem
{
    [Dependency] private readonly SpriteSystem _spriteSystem = default!;
    public bool Enabled = false;
    public bool ShowCAS = false;

    public override void Update(float frameTime)
    {
        if (!Enabled)
            return;

        var areas = AllEntityQuery<AreaComponent, SpriteComponent>();
        while (areas.MoveNext(out var uid, out var area, out var sprite))
        {
            _spriteSystem.SetVisible((uid, sprite), area.CAS == ShowCAS);
        }
    }
}

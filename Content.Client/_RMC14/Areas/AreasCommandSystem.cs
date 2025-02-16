using Content.Shared._RMC14.Areas;
using Robust.Client.GameObjects;

namespace Content.Client._RMC14.Areas;

public sealed class AreasCommandSystem : EntitySystem
{
    public bool Enabled = false;
    public bool ShowCAS = false;

    public override void Update(float frameTime)
    {
        if (!Enabled)
            return;

        var areas = AllEntityQuery<AreaComponent, SpriteComponent>();
        while (areas.MoveNext(out var area, out var sprite))
        {
            sprite.Visible = area.CAS == ShowCAS;
        }
    }
}

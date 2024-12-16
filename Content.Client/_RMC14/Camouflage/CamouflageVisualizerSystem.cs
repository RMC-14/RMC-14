using Robust.Shared.Utility;
using Content.Shared._RMC14.Item;
using Robust.Client.GameObjects;



namespace Content.Client._RMC14.Camouflage;

public sealed class CamouflageVisualizerSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CamouflageVisualizerComponent, ItemCamouflageEvent>(OnCamoChange);
    }

    private void OnCamoChange(Entity<CamouflageVisualizerComponent> ent, ref ItemCamouflageEvent args)
    {
        if (!TryComp<SpriteComponent>(ent, out var sprite))
            return;

    }
}

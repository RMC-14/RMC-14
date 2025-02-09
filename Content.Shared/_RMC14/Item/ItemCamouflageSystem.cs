using Robust.Shared.Network;

namespace Content.Shared._RMC14.Item;

public sealed class ItemCamouflageSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly INetManager _net = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    public CamouflageType CurrentMapCamouflage { get; set; } = CamouflageType.Jungle;

    private readonly Queue<Entity<ItemCamouflageComponent>> _items = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<ItemCamouflageComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ItemCamouflageComponent> ent, ref MapInitEvent args)
    {
        if (_net.IsClient)
            return;

        _items.Enqueue(ent);
    }

    public override void Update(float frameTime)
    {
        if (_items.Count == 0)
            return;

        while (_items.TryDequeue(out var ent))
        {
            _appearance.SetData(ent, ItemCamouflageVisuals.Camo, CurrentMapCamouflage);
        }
    }
}

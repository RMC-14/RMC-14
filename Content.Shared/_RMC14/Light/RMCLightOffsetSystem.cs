using System.Numerics;
using Content.Shared._RMC14.Sprite;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Light;

public sealed class RMCLightOffsetSystem : EntitySystem
{
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedRMCSpriteSystem _sprite = default!;

    protected readonly HashSet<EntityUid> ToUpdate = new();


    public override void Initialize()
    {
        SubscribeLocalEvent<RMCLightOffsetComponent, MapInitEvent>(OnLightUpdate);
        SubscribeLocalEvent<RMCLightOffsetComponent, EntParentChangedMessage>(OnLightUpdate);
    }

    private void OnLightUpdate<T>(Entity<RMCLightOffsetComponent> ent, ref T args)
    {
        if (!TryComp(ent, out MetaDataComponent? metaData) ||
            metaData.EntityLifeStage < EntityLifeStage.MapInitialized)
        {
            return;
        }

        ToUpdate.Add(ent);

        if (_net.IsClient)
            return;

        if (TerminatingOrDeleted(ent))
            return;

        var sprite = EnsureComp<SpriteSetRenderOrderComponent>(ent);
        switch (Transform(ent).LocalRotation.GetDir())
        {
            case Direction.South:
                _sprite.SetOffset(ent, new Vector2(0.45f, -0.32f));
                break;
            case Direction.East:
                _sprite.SetOffset(ent, new Vector2(0.7f, -1.45f));
                break;
            case Direction.North:
                _sprite.SetOffset(ent, new Vector2(-0.5f, -1.5f));
                break;
            case Direction.West:
                _sprite.SetOffset(ent, new Vector2(-0.7f, -0.4f));
                break;
        }

        Dirty(ent, sprite);
    }

}

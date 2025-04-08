using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Tools.Systems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Random;

namespace Content.Shared._RMC14.Crate;

public sealed class CrateOpenableSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedToolSystem _tool = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<CrateOpenableComponent, InteractUsingEvent>(OnInteractUsing);
    }

    private void OnInteractUsing(Entity<CrateOpenableComponent> ent, ref InteractUsingEvent args)
    {
        if (EntityManager.IsQueuedForDeletion(ent))
            return;

        if (!_tool.HasQuality(args.Used, ent.Comp.Tool))
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.WrongToolPopup), ent, args.User, PopupType.SmallCaution);
            return;
        }

        args.Handled = true;
        _audio.PlayPredicted(ent.Comp.Sound, _transform.GetMoverCoordinates(ent), args.User);

        if (_net.IsClient)
            return;

        QueueDel(ent);

        var spawns = EntitySpawnCollection.GetSpawns(ent.Comp.Spawn, _random);
        foreach (var spawn in spawns)
        {
            TrySpawnNextTo(spawn, ent, out _);
        }
    }
}

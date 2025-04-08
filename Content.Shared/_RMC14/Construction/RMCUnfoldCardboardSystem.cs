using Content.Shared._RMC14.Inventory;
using Content.Shared._RMC14.Weapons.Ranged.Ammo.BulletBox;
using Content.Shared.Database;
using Content.Shared.Popups;
using Content.Shared.Stacks;
using Content.Shared.Storage;
using Content.Shared.Verbs;
using Robust.Shared.Network;

namespace Content.Shared._RMC14.Construction;

public sealed class RMCUnfoldCardboardSystem : EntitySystem
{
    [Dependency] private readonly SharedCMInventorySystem _cmInventory = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedStackSystem _stack = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RMCUnfoldCardboardComponent, GetVerbsEvent<Verb>>(OnGetVerbs);
    }

    private void OnGetVerbs(Entity<RMCUnfoldCardboardComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || args.Hands == null)
            return;

        var user = args.User;

        var v = new Verb
        {
            Priority = 1,
            Text = Loc.GetString(ent.Comp.VerbText),
            Impact = LogImpact.Low,
            DoContactInteraction = true,
            Act = () =>
            {
                UnfoldCardboard(ent, user);
            },
        };

        args.Verbs.Add(v);
    }

    private void UnfoldCardboard(Entity<RMCUnfoldCardboardComponent> ent, EntityUid user)
    {
        void NotEmptyPopup()
        {
            _popup.PopupClient(Loc.GetString(ent.Comp.FailedNotEmptyText, ("entityName", ent.Owner)), ent, user);
        }

        if (_cmInventory.GetItemSlotsFilled(ent.Owner).Filled != 0)
        {
            NotEmptyPopup();
            return;
        }

        if (TryComp(ent, out BulletBoxComponent? bulletBox) && bulletBox.Amount > 0)
        {
            NotEmptyPopup();
            return;
        }

        if (TryComp(ent, out StorageComponent? storage) && storage.Container.Count > 0)
        {
            NotEmptyPopup();
            return;
        }

        if (!_net.IsServer)
            return;

        foreach (var spawn in EntitySpawnCollection.GetSpawns(ent.Comp.Spawns))
        {
            var spawned = SpawnNextToOrDrop(spawn, ent);
            _stack.TryMergeToHands(spawned, user);
        }

        Del(ent);
    }
}

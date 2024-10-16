using Content.Server.NameIdentifier;
using Content.Shared._RMC14.Xenonids.Evolution;
using Content.Shared.NameIdentifier;
using Robust.Shared.Random;

namespace Content.Server._RMC14.NameIdentifier;

public sealed class RMCNameIdentifierSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly NameIdentifierSystem _nameIdentifier = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<NameIdentifierComponent, NewXenoEvolvedEvent>(OnNewXenoEvolved);
        SubscribeLocalEvent<TransferNameIdentifierComponent, AfterNewXenoEvolvedEvent>(OnAfterNewXenoEvolved);
    }

    private void OnNewXenoEvolved(Entity<NameIdentifierComponent> ent, ref NewXenoEvolvedEvent args)
    {
        if (!TryComp(args.OldXeno, out NameIdentifierComponent? nameIdentifier))
            return;

        var transfer = EnsureComp<TransferNameIdentifierComponent>(ent);
        transfer.FullIdentifier = nameIdentifier.FullIdentifier;
        transfer.Identifier = nameIdentifier.Identifier;
        transfer.Group = nameIdentifier.Group;
    }

    private void OnAfterNewXenoEvolved(Entity<TransferNameIdentifierComponent> ent, ref AfterNewXenoEvolvedEvent args)
    {
        var entityName = Name(ent);
        if (TryComp(ent, out NameIdentifierComponent? nameIdentifier))
        {
            entityName = entityName.Replace(nameIdentifier.FullIdentifier, string.Empty).Trim();
            ReturnIdentifier(nameIdentifier.Group, nameIdentifier.Identifier);
        }

        var identifier = EnsureComp<NameIdentifierComponent>(ent);
        identifier.FullIdentifier = ent.Comp.FullIdentifier;
        identifier.Identifier = ent.Comp.Identifier;
        identifier.Group = ent.Comp.Group;

        _metaData.SetEntityName(ent, $"{entityName} {ent.Comp.FullIdentifier}");

        if (_nameIdentifier.CurrentIds.TryGetValue(ent.Comp.Group, out var group))
        {
            for (var i = group.Count - 1; i >= 0; i--)
            {
                var id = group[i];
                if (id == ent.Comp.Identifier)
                    group.RemoveAt(i);
            }
        }
    }

    private void ReturnIdentifier(string group, int identifier)
    {
        if (_nameIdentifier.CurrentIds.TryGetValue(group, out var ids))
        {
            // Avoid inserting the value right back at the end or shuffling in place:
            // just pick a random spot to put it and then move that one to the end.
            var randomIndex = _random.Next(ids.Count);
            var random = ids[randomIndex];
            ids[randomIndex] = identifier;
            ids.Add(random);
        }
    }
}

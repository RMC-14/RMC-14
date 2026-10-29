using Content.Shared._RMC14.Attachable.Components;
using Content.Shared.Examine;
using Robust.Shared.Containers;

namespace Content.Shared._RMC14.Attachable.Systems;

public sealed class AttachableHolderExamineSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableHolderComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<AttachableHolderComponent> holder, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        var metaQuery = GetEntityQuery<MetaDataComponent>();

        using (args.PushGroup(nameof(AttachableHolderExamineSystem), -10))
        {
            args.PushMarkup(Loc.GetString("rmc-attachable-examine-header"));

            foreach (var (slotId, _) in holder.Comp.Slots)
            {
                var slotName = Loc.GetString(slotId);

                if (_container.TryGetContainer(holder.Owner, slotId, out var container) &&
                    container.ContainedEntities.Count > 0)
                {
                    var entity = container.ContainedEntities[0];
                    var installedName = metaQuery.GetComponent(entity).EntityName;

                    args.PushMarkup(Loc.GetString("rmc-attachable-examine-slot-filled",
                        ("slot", slotName),
                        ("attachment", installedName)));
                }
                else
                {
                    args.PushMarkup(Loc.GetString("rmc-attachable-examine-slot-empty",
                        ("slot", slotName)));
                }
            }
        }
    }
}

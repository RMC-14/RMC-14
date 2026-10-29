using Content.Client._RMC14.Attachable.Ui;
using Content.Shared._RMC14.Attachable.Components;
using Content.Shared.Verbs;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client._RMC14.Attachable.Systems;

public sealed class AttachableHolderExamineSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    private bool _menuOpen = false;

    public override void Initialize()
    {
        SubscribeLocalEvent<AttachableHolderComponent, GetVerbsEvent<Verb>>(OnVerb);
    }

    private void OnVerb(Entity<AttachableHolderComponent> holder, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanInteract && !args.CanAccess)
            return;

        foreach (var verb in args.Verbs)
        {
            if (verb.Text == Loc.GetString("rmc-attachable-examine-verb-text"))
                return;
        }

        var slots = holder.Comp.Slots;

        args.Verbs.Add(new Verb
        {
            Text = Loc.GetString("rmc-attachable-examine-verb-text"),
            Icon = new SpriteSpecifier.Texture(new ResPath("/Textures/Interface/VerbIcons/information.svg.192dpi.png")),
            Act = () =>
            {
                if (_menuOpen)
                    return;

                var displaySlots = new Dictionary<string, List<string>>();

                foreach (var (slotId, slot) in slots)
                {
                    var slotName = Loc.GetString(slotId);
                    var entries = new List<string>();

                    if (slot.Whitelist?.Tags != null)
                    {
                        foreach (var tag in slot.Whitelist.Tags)
                        {
                            if (_prototype.TryIndex<EntityPrototype>(tag, out var proto))
                                entries.Add(proto.Name);
                            else
                                entries.Add(tag);
                        }
                    }

                    if (entries.Count > 0)
                        displaySlots[slotName] = entries;
                }

                var menu = new AttachableHolderExamineMenu();
                _menuOpen = true;
                menu.OnClose += () => _menuOpen = false;
                menu.UpdateSlots(displaySlots);
                menu.OpenCentered();
            },
            Priority = -1,
        });
    }
}

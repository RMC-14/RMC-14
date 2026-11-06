using Content.Shared.Wires;

// ReSharper disable CheckNamespace
namespace Content.Server.Wires;

public sealed partial class WiresSystem
{
    public void SetWiresCut(Entity<WiresComponent> ent, EntityUid user, bool cut)
    {
        foreach (var wire in ent.Comp.WiresList)
        {
            if (wire.IsCut == cut)
                continue;

            if (wire.Action != null && !(cut ? wire.Action.Cut(user, wire) : wire.Action.Mend(user, wire)))
                continue;

            wire.IsCut = cut;
            wire.Action?.Update(wire);
        }

        UpdateUserInterface(ent, ent.Comp);
    }
}

using Content.Shared._RMC14.Mobs.Animals;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCRodentSystem
{
    private void TryRodentAmbientSqueak(Entity<RMCRodentBehaviorComponent> ent, TimeSpan now)
    {
        if (ent.Comp.NextSqueakAt > now ||
            !Random.Prob(ent.Comp.AmbientSqueakChance))
        {
            return;
        }

        ent.Comp.NextSqueakAt = now + ent.Comp.SqueakCooldown;
        _audio.PlayPvs(ent.Comp.SqueakSound, ent.Owner);
        Popup.PopupEntity(Loc.GetString("rmc-rodent-squeaks", ("rodent", ent.Owner)), ent.Owner);
    }
}

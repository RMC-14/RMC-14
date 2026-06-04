using Content.Shared._RMC14.Mobs.Animals;
using Robust.Shared.Random;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void PlayGrowl(Entity<RMCGiantLizardComponent> ent)
    {
        if (ent.Comp.NextGrowlAt > Timing.CurTime)
            return;

        ent.Comp.NextGrowlAt = Timing.CurTime + RandomTime(ent.Comp.GrowlCooldownMin, ent.Comp.GrowlCooldownMax);
        _audio.PlayPvs(ent.Comp.GrowlSound, ent.Owner);
    }

    private string PickFriendlyPetPopup()
    {
        return Random.Pick(new[]
        {
            "rmc-giant-lizard-pet-happy",
            "rmc-giant-lizard-pet-nuzzle",
            "rmc-giant-lizard-pet-lick",
            "rmc-giant-lizard-pet-stare",
        });
    }
}

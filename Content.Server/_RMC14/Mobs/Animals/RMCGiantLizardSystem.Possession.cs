using Content.Shared._RMC14.Mobs.Animals;

namespace Content.Server._RMC14.Mobs.Animals;

public sealed partial class RMCGiantLizardSystem
{
    private void UpdatePossession(Entity<RMCGiantLizardComponent, TransformComponent> ent)
    {
        if (ActorQuery.HasComp(ent.Owner))
        {
            WakeRest((ent.Owner, ent.Comp1));

            if (!ent.Comp1.SleepingForPossession)
            {
                RMCNpc.SleepNPC(ent.Owner);
                ent.Comp1.SleepingForPossession = true;
            }

            return;
        }

        if (!ent.Comp1.SleepingForPossession)
            return;

        RMCNpc.WakeNPC(ent.Owner);
        ent.Comp1.SleepingForPossession = false;
    }
}

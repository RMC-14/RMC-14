using Content.Shared.Body.Systems;

namespace Content.Shared._RMC14.Medical.Wounds;

public sealed partial class RMCBloodSplatterSystem : EntitySystem
{
    [Dependency] private readonly SharedWoundsSystem _wounds = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<RMCBloodSplattererComponent, RMCWoundAddedEvent>(OnWoundAdded);
    }

    //TODO: Splatters come from:
    // wound formation (always big)
    // blood loss (>10 big, else small)

    public void OnWoundAdded(Entity<RMCBloodSplattererComponent> ent, ref RMCWoundAddedEvent args)
    {
        if (args.Wound.Damage < ent.Comp.MinimalTriggerDamage)
        {
            Log.Debug("Adding small blood spatter");
            return;
        }

        Log.Debug("Adding large splatter");
    }

    public void SpawnDecal()
    {
        
    }


}

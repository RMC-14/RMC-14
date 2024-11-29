using Content.Shared._RMC14.Xenonids.Projectile.Parasite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Content.Shared._RMC14.Xenonids.Construction.EggMorpher;

public sealed partial class EggMorpherSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EggMorpherComponent, XenoChangeParasiteReserveMessage>(OnChangeParasiteReserve);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);


    }

    private void OnChangeParasiteReserve(Entity<EggMorpherComponent> eggMorpher, ref XenoChangeParasiteReserveMessage args)
    {
        eggMorpher.Comp.ReservedParasites = args.NewReserve;
    }
}

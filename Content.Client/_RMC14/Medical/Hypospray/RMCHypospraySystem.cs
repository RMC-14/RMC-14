using Content.Client.Items;
using Content.Shared._RMC14.Chemistry;

namespace Content.Client._RMC14.Medical.Hypospray;

public sealed class RMCHypospraySystem : RMCSharedHypospraySystem
{
    public override void Initialize()
    {
        base.Initialize();
        Subs.ItemStatus<RMCHyposprayComponent>(ent => new RMCHyposprayStatusControl(ent, _solution, _container));
    }
}

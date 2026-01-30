using Content.Shared.Administration.Logs;
using Content.Shared.Database;
using Content.Shared.DetailExaminable;
using Content.Shared.GameTicking;

namespace Content.Shared._RMC14.DetailExaminable;

public sealed class RMCDetailedExaminableSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLog = default!;

    private readonly List<Entity<DetailExaminableComponent>> _queue = new();

    public override void Initialize()
    {
        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);

        SubscribeLocalEvent<DetailExaminableComponent, MapInitEvent>(OnDetailExaminableMapInit);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent ev)
    {
        _queue.Clear();
    }

    private void OnDetailExaminableMapInit(Entity<DetailExaminableComponent> ent, ref MapInitEvent args)
    {
        _queue.Add(ent);
    }

    public override void Update(float frameTime)
    {
        try
        {
            foreach (var ent in _queue)
            {
                _adminLog.Add(LogType.RMCCharacterDescription, $"{ToPrettyString(ent):player} had a character description added:\n{ent.Comp.Content:description}");
            }
        }
        finally
        {
            _queue.Clear();
        }
    }
}

using Content.Shared.Destructible;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Robust.Shared.Containers;
using Robust.Shared.Player;

namespace Content.Shared._RMC14.Xenonids.Parasite;

public abstract partial class SharedXenoParasiteSystem
{
    public void IntializeAI()
    {
        SubscribeLocalEvent<XenoParasiteComponent, PlayerAttachedEvent>(OnPlayerAdded);
        SubscribeLocalEvent<XenoParasiteComponent, PlayerDetachedEvent>(OnPlayerRemoved);

        SubscribeLocalEvent<ParasiteAIComponent, ComponentStartup>(OnAIAdded);
        SubscribeLocalEvent<ParasiteAIComponent, ExaminedEvent>(OnAIExamined);
        SubscribeLocalEvent<ParasiteAIComponent, DroppedEvent>(OnAIDropPickup);
        SubscribeLocalEvent<ParasiteAIComponent, EntGotInsertedIntoContainerMessage>(OnAIDropPickup);
    }

    private void OnPlayerAdded(Entity<XenoParasiteComponent> para, ref PlayerAttachedEvent args)
    {
        RemCompDeferred<ParasiteAIComponent>(para);
    }

    private void OnPlayerRemoved(Entity<XenoParasiteComponent> para, ref PlayerDetachedEvent args)
    {
        EnsureComp<ParasiteAIComponent>(para);
    }

    private void OnAIAdded(Entity<ParasiteAIComponent> para, ref ComponentStartup args)
    {
        HandleDeathTimer(para);
    }

    private void OnAIExamined(Entity<ParasiteAIComponent> para, ref ExaminedEvent args)
    {
        if (_mobState.IsDead(para) || !HasComp<XenoComponent>(args.Examiner))
            return;

        switch (para.Comp.Mode)
        {
            case ParasiteMode.Idle:
                args.PushMarkup($"{Loc.GetString("rmc-xeno-parasite-ai-idle", ("parasite", para))}");
                break;
            case ParasiteMode.Active:
                args.PushMarkup($"{Loc.GetString("rmc-xeno-parasite-ai-active", ("parasite", para))}");
                break;
            case ParasiteMode.Dying:
                args.PushMarkup($"{Loc.GetString("rmc-xeno-parasite-ai-dying", ("parasite", para))}");
                break;
        }
    }


    private void OnAIDropPickup<T>(Entity<ParasiteAIComponent> para, ref T args) where T : EntityEventArgs
    {
        HandleDeathTimer(para);
        //Go Idle
    }

    public void HandleDeathTimer(Entity<ParasiteAIComponent> para)
    {
        if (_container.TryGetContainingContainer((para, null, null), out var carry) && HasComp<XenoComponent>(carry.Owner)) // TODO Check for parasite thrower
        {
            para.Comp.DeathTime = null;
            //Go Idle
            return;
        }

        if (para.Comp.DeathTime == null)
            para.Comp.DeathTime = _timing.CurTime + para.Comp.LifeTime;
    }

    public void CheckTimers(Entity<ParasiteAIComponent> para, TimeSpan currentTime)
    {

    }
}

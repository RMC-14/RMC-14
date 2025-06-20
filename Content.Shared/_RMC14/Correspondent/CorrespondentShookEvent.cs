namespace Content.Shared._RMC14.Correspondant;

public sealed class CorrespondentShookEvent : EntityEventArgs
{
    public EntityUid Correspondent;

    public CorrespondentShookEvent(EntityUid Corresp)
    {
        Correspondent = Corresp;
    }
}

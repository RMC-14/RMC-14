using Robust.Shared.Serialization;

namespace Content.Shared._RMC14.Mentor;

[Serializable, NetSerializable]
public sealed class SendMentorHelpMessageEvent(string message) : EntityEventArgs
{
    public readonly string Message = message;
}

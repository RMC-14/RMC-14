namespace Content.Shared._RMC14.Attachable.Events;

[ByRefEvent]
public readonly record struct AttachableGetExamineDataEvent(Dictionary<byte, (AttachableModifierConditions? conditions, List<string> effectStrings)> Data);

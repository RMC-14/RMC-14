using Content.Server._RMC14.Announce.Core;
using Content.Shared._RMC14.Announce;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Content.Server._RMC14.Announce.Validation;

public sealed class AnnouncementValidator
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    private const int MaxMessageLength = 1000;
    private const int MaxLineCount = 10;

    public AnnouncementValidator()
    {
        IoCManager.InjectDependencies(this);
    }

    public ValidationResult ValidateRequest(AnnouncementRequest request)
    {
        var result = new ValidationResult();

        ValidateMessage(request.Message, result);
        ValidateEntities(request, result);
        ValidateParameters(request, result);

        return result;
    }

    private void ValidateMessage(string message, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            result.AddError("Message cannot be empty");
            return;
        }

        if (message.Length > MaxMessageLength)
        {
            result.AddError($"Message too long. Maximum {MaxMessageLength} characters, got {message.Length}");
        }

        var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
        if (!normalized.Contains('\n') && normalized.Contains("\\n"))
            normalized = normalized.Replace("\\n", "\n");

        var lines = normalized.Split('\n');
        if (lines.Length > MaxLineCount)
        {
            result.AddError($"Too many lines. Maximum {MaxLineCount} lines, got {lines.Length}");
        }
    }

    private void ValidateEntities(AnnouncementRequest request, ValidationResult result)
    {
        if (request.Speaker.HasValue && !_entityManager.EntityExists(request.Speaker.Value))
        {
            result.AddError($"Speaker entity {request.Speaker.Value} does not exist");
        }

        if (request.Source.HasValue && !_entityManager.EntityExists(request.Source.Value))
        {
            result.AddError($"Source entity {request.Source.Value} does not exist");
        }

    }

    private void ValidateParameters(AnnouncementRequest request, ValidationResult result)
    {
        if (request.VolumeOverride.HasValue)
        {
            var volume = request.VolumeOverride.Value;
            if (volume < 0f || volume > 2f)
            {
                result.AddError($"Volume must be between 0.0 and 2.0, got {volume}");
            }
        }

        if (request.PriorityOverride.HasValue)
        {
            var priority = request.PriorityOverride.Value;
            if (priority < 0f || priority > 10f)
            {
                result.AddError($"Priority must be between 0.0 and 10.0, got {priority}");
            }
        }

        if (request.SpriteScale < 0.1f || request.SpriteScale > 5f)
        {
            result.AddError($"Sprite scale must be between 0.1 and 5.0, got {request.SpriteScale}");
        }
    }
}

public sealed class ValidationResult
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<string> Errors => _errors;
    public IReadOnlyList<string> Warnings => _warnings;

    public void AddError(string error)
    {
        _errors.Add(error);
    }

    public void AddWarning(string warning)
    {
        _warnings.Add(warning);
    }

    public string GetErrorSummary()
    {
        if (IsValid) return "Valid";
        return string.Join(", ", _errors);
    }
}

using System.Numerics;

namespace Content.Shared._RMC14.Hallucination;

public sealed class RMCHallucinationParasiteChase(
    Vector2 position,
    float runSpeed,
    float leapRange,
    TimeSpan leapDelay,
    TimeSpan leapTime,
    TimeSpan giveUpAt)
{
    public RMCHallucinationParasitePhase Phase = RMCHallucinationParasitePhase.Running;
    public Vector2 Position = position;
    public TimeSpan GiveUpAt = giveUpAt;

    private Vector2 _leapFrom;
    private TimeSpan _phaseStart;

    public void Offset(TimeSpan by)
    {
        GiveUpAt += by;
        _phaseStart += by;
    }

    public bool Update(Vector2 target, TimeSpan time, float frameTime)
    {
        switch (Phase)
        {
            case RMCHallucinationParasitePhase.Running:
            {
                var delta = target - Position;
                var distance = delta.Length();
                if (distance <= leapRange)
                {
                    Phase = RMCHallucinationParasitePhase.Waiting;
                    _phaseStart = time;
                    break;
                }

                Position += delta / distance * MathF.Min(runSpeed * frameTime, distance - leapRange);
                break;
            }
            case RMCHallucinationParasitePhase.Waiting:
            {
                if (time < _phaseStart + leapDelay)
                    break;

                Phase = RMCHallucinationParasitePhase.Leaping;
                _phaseStart = time;
                _leapFrom = Position;
                break;
            }
            case RMCHallucinationParasitePhase.Leaping:
            {
                var progress = (float) ((time - _phaseStart) / leapTime);
                if (progress >= 1)
                {
                    Position = target;
                    Phase = RMCHallucinationParasitePhase.None;
                    return true;
                }

                Position = Vector2.Lerp(_leapFrom, target, progress);
                break;
            }
        }

        return false;
    }
}

public enum RMCHallucinationParasitePhase : byte
{
    None,
    Running,
    Waiting,
    Leaping,
}

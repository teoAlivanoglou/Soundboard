namespace SonicPad;

/// <summary>Guards the one-time splash handoff against late asynchronous completion.</summary>
public sealed class StartupWindowTransition
{
    public bool IsStopped { get; private set; }
    public bool IsRevealed { get; private set; }

    public bool TryReveal()
    {
        if (IsStopped || IsRevealed) return false;
        IsRevealed = true;
        return true;
    }

    public void Stop() => IsStopped = true;
}

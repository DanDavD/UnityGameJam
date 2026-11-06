namespace GameJam.Core
{
    /// <summary>Source of randomness, injected so tests and simulations can be deterministic.</summary>
    public interface IRandom
    {
        int Range(int minInclusive, int maxExclusive);
    }
}

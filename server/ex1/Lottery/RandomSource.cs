using System.Security.Cryptography;

namespace ex1.Lottery;

/// <summary>Abstracts randomness so the draw can be tested deterministically.</summary>
public interface IRandomSource
{
    /// <summary>Returns a uniformly distributed integer in [0, maxExclusive).</summary>
    int NextInt(int maxExclusive);
}

/// <summary>
/// Cryptographically secure randomness. Unlike <see cref="Random"/>, the outcome cannot be
/// predicted from earlier results, which matters when real prizes depend on it.
/// </summary>
public sealed class CryptoRandomSource : IRandomSource
{
    public int NextInt(int maxExclusive) => RandomNumberGenerator.GetInt32(maxExclusive);
}

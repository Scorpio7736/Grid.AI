using System.Buffers.Binary;
using System.Security.Cryptography;
namespace Grid.Simulation.Behavior;

/// <summary>Version-one stateless seeded choice: stable across restore, runtimes and enumeration order.</summary>
public static class DecisionRandom
{
    public static ulong Rank(Guid worldSeed, long decision, Guid actor, Guid candidate)
    {
        Span<byte> input = stackalloc byte[56];
        worldSeed.TryWriteBytes(input[..16]);
        BinaryPrimitives.WriteInt64LittleEndian(input[16..24], decision);
        actor.TryWriteBytes(input[24..40]);
        candidate.TryWriteBytes(input[40..56]);
        Span<byte> digest = stackalloc byte[32];
        SHA256.HashData(input, digest);
        return BinaryPrimitives.ReadUInt64LittleEndian(digest);
    }
}

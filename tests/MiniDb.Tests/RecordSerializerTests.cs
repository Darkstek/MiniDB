using MiniDb.Engine.Log;

namespace MiniDb.Tests;

public class RecordSerializerTests
{
    [Fact]
    public void Set_record_survives_round_trip()
    {
        var original = LogRecord.Set("user:1", "Daniel");

        byte[] bytes = RecordSerializer.Serialize(original);
        LogRecord restored = RecordSerializer.Deserialize(bytes);

        Assert.Equal(original, restored);
    }

    [Fact]
    public void Tombstone_record_survives_round_trip()
    {
        var original = LogRecord.Tombstone("user:1");

        byte[] bytes = RecordSerializer.Serialize(original);
        LogRecord restored = RecordSerializer.Deserialize(bytes);

        Assert.Equal("user:1", restored.Key);
        Assert.Null(restored.Value);
        Assert.Equal(RecordType.Tombstone, restored.Type);
    }

    [Fact]
    public void Corrupted_record_throws()
    {
        var original = LogRecord.Set("k", "v");
        byte[] bytes = RecordSerializer.Serialize(original);

        bytes[^1] ^= 0xFF;   // překlopíme poslední bajt = simulace poškození

        Assert.Throws<InvalidDataException>(() => RecordSerializer.Deserialize(bytes));
    }
}
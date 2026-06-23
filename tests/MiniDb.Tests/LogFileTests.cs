using MiniDb.Engine.Log;

namespace MiniDb.Tests;

public class LogFileTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"minidb-test-{Guid.NewGuid()}.log");

    [Fact]
    public void Records_can_be_appended_and_read_back_by_offset()
    {
        long offset1, offset2;

        using (var writer = new LogWriter(_path))
        {
            offset1 = writer.Append(LogRecord.Set("a", "apple"));
            offset2 = writer.Append(LogRecord.Set("b", "banana"));
        }

        using var reader = new LogReader(_path);

        LogRecord first = reader.ReadAt(offset1);
        LogRecord second = reader.ReadAt(offset2);

        Assert.Equal("apple", first.Value);
        Assert.Equal("banana", second.Value);
    }

    [Fact]
    public void First_record_starts_at_offset_zero()
    {
        using var writer = new LogWriter(_path);
        long offset = writer.Append(LogRecord.Set("k", "v"));
        Assert.Equal(0L, offset);
    }

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
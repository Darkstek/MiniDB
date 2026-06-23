using MiniDb.Engine;

namespace MiniDb.Tests;

public class RecoveryTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"minidb-recovery-{Guid.NewGuid()}.log");

    [Fact]
    public void Index_is_rebuilt_after_reopen()
    {
        using (var db = new KvStore(_path))
        {
            db.Set("a", "apple");
            db.Set("b", "banana");
            db.Set("a", "avocado");   // přepis
            db.Delete("b");           // smazání
        }

        using var reopened = new KvStore(_path);

        Assert.Equal("avocado", reopened.Get("a"));
        Assert.Null(reopened.Get("b"));
    }

    [Fact]
    public void Recovery_stops_at_corrupted_trailing_record()
    {
        using (var db = new KvStore(_path))
        {
            db.Set("a", "apple");
            db.Set("b", "banana");
        }

        File.AppendAllText(_path, "halfwritten");   // simulace pádu uprostřed zápisu

        using var reopened = new KvStore(_path);

        Assert.Equal("apple", reopened.Get("a"));
        Assert.Equal("banana", reopened.Get("b"));
    }

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
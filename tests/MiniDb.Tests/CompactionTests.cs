using MiniDb.Engine;

namespace MiniDb.Tests;

public class CompactionTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"minidb-compact-{Guid.NewGuid()}.log");

    [Fact]
    public void Data_is_intact_after_compaction()
    {
        using var db = new KvStore(_path);
        db.Set("a", "apple");
        db.Set("b", "banana");
        db.Set("a", "avocado");
        db.Delete("b");

        db.Compact();

        Assert.Equal("avocado", db.Get("a"));
        Assert.Null(db.Get("b"));
    }

    [Fact]
    public void Compaction_shrinks_the_log_file()
    {
        using var db = new KvStore(_path);

        for (int i = 0; i < 100; i++)
            db.Set("k", $"value-{i}");

        long before = new FileInfo(_path).Length;
        db.Compact();
        long after = new FileInfo(_path).Length;

        Assert.True(after < before);
        Assert.Equal("value-99", db.Get("k"));
    }

    [Fact]
    public void Survives_reopen_after_compaction()
    {
        using (var db = new KvStore(_path))
        {
            db.Set("x", "1");
            db.Set("y", "2");
            db.Set("x", "3");
            db.Compact();
        }

        using var reopened = new KvStore(_path);
        Assert.Equal("3", reopened.Get("x"));
        Assert.Equal("2", reopened.Get("y"));
    }

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
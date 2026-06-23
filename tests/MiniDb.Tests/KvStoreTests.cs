using MiniDb.Engine;

namespace MiniDb.Tests;

public class KvStoreTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"minidb-kv-{Guid.NewGuid()}.log");

    [Fact]
    public void Set_then_Get_returns_value()
    {
        using var db = new KvStore(_path);
        db.Set("user:1", "Daniel");
        Assert.Equal("Daniel", db.Get("user:1"));
    }

    [Fact]
    public void Get_missing_key_returns_null()
    {
        using var db = new KvStore(_path);
        Assert.Null(db.Get("nope"));
    }

    [Fact]
    public void Set_twice_returns_latest_value()
    {
        using var db = new KvStore(_path);
        db.Set("k", "first");
        db.Set("k", "second");
        Assert.Equal("second", db.Get("k"));
    }

    [Fact]
    public void Delete_removes_key()
    {
        using var db = new KvStore(_path);
        db.Set("k", "v");
        db.Delete("k");
        Assert.Null(db.Get("k"));
    }

    [Fact]
    public void Multiple_keys_are_independent()
    {
        using var db = new KvStore(_path);
        db.Set("a", "1");
        db.Set("b", "2");
        Assert.Equal("1", db.Get("a"));
        Assert.Equal("2", db.Get("b"));
    }

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
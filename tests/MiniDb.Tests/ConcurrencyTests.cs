using MiniDb.Engine;

namespace MiniDb.Tests;

public class ConcurrencyTests : IDisposable
{
    private readonly string _path =
        Path.Combine(Path.GetTempPath(), $"minidb-conc-{Guid.NewGuid()}.log");

    [Fact]
    public void Concurrent_writes_do_not_corrupt_data()
    {
        using var db = new KvStore(_path);

        Parallel.For(0, 1000, i => db.Set($"key:{i}", $"value:{i}"));

        for (int i = 0; i < 1000; i++)
            Assert.Equal($"value:{i}", db.Get($"key:{i}"));
    }

    [Fact]
    public void Concurrent_reads_and_writes_are_safe()
    {
        using var db = new KvStore(_path);

        for (int i = 0; i < 100; i++)
            db.Set($"k:{i}", $"v:{i}");

        Parallel.For(0, 2000, i =>
        {
            if (i % 2 == 0)
                db.Set($"k:{i % 100}", $"v:{i}");
            else
                _ = db.Get($"k:{i % 100}");
        });

        for (int i = 0; i < 100; i++)
            Assert.NotNull(db.Get($"k:{i}"));
    }

    public void Dispose()
    {
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
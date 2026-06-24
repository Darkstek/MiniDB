using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using MiniDb.Engine;

BenchmarkRunner.Run<KvStoreBenchmarks>();

[MemoryDiagnoser]
public class KvStoreBenchmarks
{
    private KvStore _db = null!;
    private string _path = null!;
    private int _counter;

    [GlobalSetup]
    public void Setup()
    {
        _path = Path.Combine(Path.GetTempPath(), $"minidb-bench-{Guid.NewGuid()}.log");
        _db = new KvStore(_path);

        for (int i = 0; i < 1000; i++)
            _db.Set($"key:{i}", $"value:{i}");
    }

    [Benchmark]
    public void Set() => _db.Set($"bench:{_counter++}", "some-value");

    [Benchmark]
    public string? Get() => _db.Get("key:500");

    [GlobalCleanup]
    public void Cleanup()
    {
        _db.Dispose();
        if (File.Exists(_path))
            File.Delete(_path);
    }
}
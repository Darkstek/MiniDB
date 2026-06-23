using MiniDb.Engine.Log;

namespace MiniDb.Engine;

public sealed class KvStore : IStorageEngine
{
    private readonly LogWriter _writer;
    private readonly LogReader _reader;
    private readonly Dictionary<string, long> _index;

    public KvStore(string path)
    {
        _writer = new LogWriter(path);
        _reader = new LogReader(path);
        _index = new Dictionary<string, long>();
    }

    public void Set(string key, string value)
    {
        var record = LogRecord.Set(key, value);
        long offset = _writer.Append(record);
        _index[key] = offset;
    }

    public string? Get(string key)
    {
        if (!_index.TryGetValue(key, out long offset))
            return null;

        LogRecord record = _reader.ReadAt(offset);
        return record.Value;
    }

    public void Delete(string key)
    {
        if (!_index.ContainsKey(key))
            return;

        _writer.Append(LogRecord.Tombstone(key));
        _index.Remove(key);
    }

    public void Dispose()
    {
        _writer.Dispose();
        _reader.Dispose();
    }
}
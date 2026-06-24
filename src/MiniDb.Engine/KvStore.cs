using MiniDb.Engine.Log;

namespace MiniDb.Engine;

public sealed class KvStore : IStorageEngine
{
    private readonly string _path;
    private readonly Dictionary<string, long> _index;
    private readonly ReaderWriterLockSlim _lock = new();
    private LogWriter _writer;
    private LogReader _reader;

    public KvStore(string path)
    {
        _path = path;
        _writer = new LogWriter(path);
        _reader = new LogReader(path);
        _index = new Dictionary<string, long>();
        Recover();
    }

    private void Recover()
    {
        long length = _reader.Length;
        long offset = 0;

        while (offset < length)
        {
            LogRecord record;
            int size;

            try
            {
                (record, size) = _reader.ReadRecord(offset);
            }
            catch (Exception ex) when (ex is EndOfStreamException or InvalidDataException)
            {
                break;
            }

            if (record.Type == RecordType.Tombstone)
                _index.Remove(record.Key);
            else
                _index[record.Key] = offset;

            offset += size;
        }
    }

    public void Set(string key, string value)
    {
        _lock.EnterWriteLock();
        try
        {
            var record = LogRecord.Set(key, value);
            long offset = _writer.Append(record);
            _index[key] = offset;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public string? Get(string key)
    {
        _lock.EnterReadLock();
        try
        {
            if (!_index.TryGetValue(key, out long offset))
                return null;

            LogRecord record = _reader.ReadAt(offset);
            return record.Value;
        }
        finally
        {
            _lock.ExitReadLock();
        }
    }

    public void Delete(string key)
    {
        _lock.EnterWriteLock();
        try
        {
            if (!_index.ContainsKey(key))
                return;

            _writer.Append(LogRecord.Tombstone(key));
            _index.Remove(key);
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Compact()
    {
        _lock.EnterWriteLock();
        try
        {
            string compactPath = _path + ".compact";
            var newIndex = new Dictionary<string, long>(_index.Count);

            using (var compactWriter = new LogWriter(compactPath))
            {
                foreach (string key in _index.Keys)
                {
                    LogRecord live = _reader.ReadAt(_index[key]);
                    long newOffset = compactWriter.Append(live);
                    newIndex[key] = newOffset;
                }
            }

            _writer.Dispose();
            _reader.Dispose();

            File.Move(compactPath, _path, overwrite: true);

            _writer = new LogWriter(_path);
            _reader = new LogReader(_path);

            _index.Clear();
            foreach (var entry in newIndex)
                _index[entry.Key] = entry.Value;
        }
        finally
        {
            _lock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        _writer.Dispose();
        _reader.Dispose();
        _lock.Dispose();
    }
}
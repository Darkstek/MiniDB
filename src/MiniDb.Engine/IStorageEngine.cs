namespace MiniDb.Engine;

public interface IStorageEngine : IDisposable
{
    void Set(string key, string value);
    string? Get(string key);
    void Delete(string key);
}
using MiniDb.Engine;

var builder = WebApplication.CreateBuilder(args);

string dbPath = builder.Configuration["MiniDb:Path"] ?? "minidb.log";

builder.Services.AddSingleton<KvStore>(_ => new KvStore(dbPath));
builder.Services.AddSingleton<IStorageEngine>(sp => sp.GetRequiredService<KvStore>());

var app = builder.Build();

app.MapPut("/keys/{key}", async (string key, HttpRequest request, IStorageEngine db) =>
{
    using var reader = new StreamReader(request.Body);
    string value = await reader.ReadToEndAsync();
    db.Set(key, value);
    return Results.Ok();
});

app.MapGet("/keys/{key}", (string key, IStorageEngine db) =>
{
    string? value = db.Get(key);
    return value is null ? Results.NotFound() : Results.Text(value);
});

app.MapDelete("/keys/{key}", (string key, IStorageEngine db) =>
{
    db.Delete(key);
    return Results.NoContent();
});

app.MapPost("/compact", (KvStore db) =>
{
    db.Compact();
    return Results.Ok();
});

app.Run();
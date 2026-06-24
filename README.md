# MiniDb

A small key-value database engine I built from scratch in C# — append-only log storage, an in-memory index, crash recovery, thread-safe access, and log compaction. No ORM, no embedded database doing the real work. Just the .NET standard library and the actual mechanics of how a storage engine works underneath.

I come from a JS/TS background (React, Node, Express). I built this to learn C#/.NET properly _and_ to go a level below CRUD — to actually understand how databases persist data, survive crashes, and handle concurrent access, instead of treating them as a black box.

It's a [Bitcask](https://riak.com/assets/bitcask-intro.pdf)-style design: every write is appended to a log file, and an in-memory hash index maps each key to the byte offset of its latest value. Reads are one index lookup plus one seek — no scanning.

## What it does

- `Set` / `Get` / `Delete` over string keys and values
- Persists to a single append-only log — survives restarts
- **Crash recovery**: rebuilds the index by replaying the log on startup, and stops cleanly at a half-written record left behind by a crash (every record is CRC-checked)
- **Thread-safe**: many concurrent readers, exclusive writers
- **Compaction**: rewrites the log to drop dead / overwritten / deleted records and reclaim space
- HTTP API over the engine (PUT / GET / DELETE)

## Architecture

Three projects (plus benchmarks), with dependencies all pointing inward to the engine:

```
MiniDb.Api  ──►  MiniDb.Engine  ◄──  MiniDb.Tests
                       ▲
                       │
              MiniDb.Benchmarks
```

- **MiniDb.Engine** — the actual database. A pure class library that knows nothing about HTTP or tests. Everything important lives here.
- **MiniDb.Api** — a thin ASP.NET Core layer that just forwards requests to the engine.
- **MiniDb.Tests** — xUnit, testing the engine in isolation (no server needed).
- **MiniDb.Benchmarks** — BenchmarkDotNet, another consumer of the same library.

The engine depends on nothing outward, so it can be driven by the API, the tests, the benchmarks, or a CLI tomorrow — without changing a line.

### How a write works

Serialize the record (with a CRC), append it to the end of the log, get back the byte offset it landed at, store `key → offset` in the index. Deletes append a _tombstone_ record instead of erasing anything.

### How a read works

Look the key up in the in-memory index to get its offset, seek to that offset, read the record, return the value. One lookup, one seek.

### Record format

```
[ CRC32 (4) | Timestamp (8) | Type (1) | KeyLen (4) | ValueLen (4) | Key | Value ]
```

The CRC covers everything after it, so a corrupted or half-written record is detectable on read. This is what makes recovery actually safe rather than "replay and hope".

### Recovery

The index is rebuilt on startup by replaying the log from offset 0. If the process crashed mid-write, the trailing record fails its CRC check (or hits EOF early); recovery stops there and the database opens in its last consistent state instead of crashing.

### Concurrency

A single `ReaderWriterLockSlim` guards the index and the log — shared lock for reads (many at once), exclusive for writes. Reads genuinely run in parallel because the reader uses `RandomAccess` (positioned reads with no shared file cursor), not a stateful stream.

## Tech stack

C# / .NET 10 · ASP.NET Core minimal API · xUnit · BenchmarkDotNet · `System.IO.Hashing` (CRC32). Almost everything is the standard library — that was the point: learn the mechanics, don't glue libraries together.

## Running it

**API:**

```bash
dotnet run --project src/MiniDb.Api
```

```bash
curl -X PUT  localhost:5007/keys/user1 -d "Daniel"
curl         localhost:5007/keys/user1      # -> Daniel
curl -X DELETE localhost:5007/keys/user1
```

**Tests:**

```bash
dotnet test
```

**Benchmarks** (Release is required):

```bash
dotnet run --project benchmarks/MiniDb.Benchmarks -c Release
```

## Benchmarks

Measured on [your CPU here], 1000 keys preloaded:

| Operation | Mean | Allocated |
| --------- | ---- | --------- |
| Set       | …    | …         |
| Get       | …    | …         |

`Set` is disk-bound (it flushes on every append); `Get` is an in-memory index lookup plus a single positioned read.

## Design decisions

- **Log-structured storage** — appends are sequential and fast, and the design makes crash recovery and concurrency tractable. The trade-off is space (dead records pile up), which is what compaction is for.
- **The index stores an offset, not the value** — keeps memory small; values are read from disk on demand.
- **CRC per record** — the thing that makes "crash recovery" mean something.
- **`ReaderWriterLockSlim` over a plain `lock`** — reads dominate and shouldn't block each other.

## Known limitations (v1) — things I know are missing, not things I missed

- **Recovery doesn't truncate the corrupted tail.** It stops reading at it, but the garbage stays in the file and the next write would append past it. Fix: truncate to the last valid offset (`SetLength`) after recovery.
- **Compaction is stop-the-world** — it holds the write lock for the whole rewrite. Real engines compact online.
- **In-memory index** — every key must fit in RAM (this is true of Bitcask too; LSM-trees solve it differently).
- **No range queries / ordering** — it's a hash index, so no "keys between X and Y". That needs a B-tree or a sorted structure.
- **Single log file** — no segment rotation.

## Why I built it

Mostly to learn — a new language, and a topic most juniors never touch. I wanted a project where I understand every single line, not one stitched together from snippets I couldn't explain. If you're a recruiter reading this: ask me anything about how it works, I can walk you through any part of it.

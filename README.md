# Synthient Edge
> .NET 10 | ASP.NET Core | Redis

Edge is a lightweight IP context service intended for self-hosting by clients with access to the [Synthient firehose](https://docs.synthient.com/enterprise/firehose). See the [wiki](https://github.com/synthient/edge/wiki) for configuration and API reference.

## Pipeline

1. **Source** - subscribes to a Redis pub/sub channel and consumes a stream of proxy events from the Synthient firehose.
2. **Filter** - evaluates events against configurable retention buckets (and their filters), and discards events that don't match or have exceeded their TTL.
3. **Sink** - writes matched events into Redis under time-bucketed per-IP keys.
4. **Serve** - exposes a REST API for enriched IP lookups, returning bucket counts, providers, geo, and network metadata

**Notes:**
- Repeated low-cardinality strings (bucket names & providers) are encoded as integer IDs via a Redis-backed string registry and cached in-process to reduce memory usage (try `ZRANGE {strings}:ids 0 -1`).
- The store can be backed by drop-in Redis replacements (e.g. DragonflyDB, Garnet, Valkey) as long as it supports the commands in use: Lua scripting (`EVAL`), `HGET`/`HSET`, `SADD`/`SMEMBERS`, `PEXPIREAT` (with `NX`/`GT` flags), `ZSCORE`/`ZADD`, and `INCR`.

---

> [!NOTE]
> Edge is currently in early alpha. Features and APIs are under active development. For bugs or questions, please open an issue or reach out through official channels.
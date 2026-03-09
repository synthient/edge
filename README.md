# Synthient Edge

> .NET 10 | ASP.NET Core | Redis

Edge is a high-performance, self-hosted IP context engine for [Synthient](https://synthient.com) customers (with
firehose access).
It ingests the real-time firehose stream, retains events in configurable buckets, and serves enriched per-IP lookups
over a REST API, enabling low-latency queries against your own infrastructure.

See the [wiki](https://github.com/synthient/edge/wiki) for configuration and API reference.

___

## Pipeline

1. **Source** - Subscribes to a Redis pub/sub channel and reads proxy events from the Synthient firehose.
2. **Filter** – Matches events against retention bucket filters and discards non-matching or expired events.
3. **Sink** – Writes matched events to Redis under time-bucketed per-IP keys.
4. **HTTP API** – Serves enriched IP lookups, returning bucket counts, providers, geo, and network metadata.

___

## Notes

- Repeated low-cardinality strings (bucket names & providers) are encoded as integer IDs via a Redis-backed string
  registry and cached in-process to reduce memory usage (try `ZRANGE {strings}:ids 0 -1`).
- The store can be backed by drop-in Redis replacements (e.g. DragonflyDB, Garnet, Valkey) as long as it supports the
  commands in use: Lua scripting (`EVAL`, `EVALSHA`), `HGET`/`HSET`, `SADD`/`SMEMBERS`, `PEXPIREAT` (with `NX`/`GT`
  flags), `ZSCORE`/`ZADD`, and `INCR`.

**Metrics log** (emitted every 5 seconds. `/s` is the average rate over the previous interval):

```
Ingested: 1,234/s | Processed: 1,180/s | Unmatched: 3.9% | Overflow: 0 | Lag: 0
```

| Field       | Description                                              |
|-------------|----------------------------------------------------------|
| `Ingested`  | Events received from the firehose per second             |
| `Processed` | Events written to the Redis sink per second              |
| `Unmatched` | Percentage of ingested events that matched no bucket     |
| `Overflow`  | Events dropped because the internal queue was full       |
| `Lag`       | Combined queued events across the filter and sink stages |

**Redis key layout:**

Keys are compact binary values composed of raw IP bytes followed by an optional bucket ID.

| Key                                       | Redis type | Contents                                 |
|-------------------------------------------|------------|------------------------------------------|
| `<ip: 4\|16 bytes>`                       | Set        | Integer bucket IDs with data for this IP |
| `<ip: 4\|16 bytes><bucket_id: uint16 BE>` | Hash       | Per-provider activity for this IP+bucket |

Hash fields are integer provider IDs. Values are 6 bytes packed big-endian:
`[count: uint16][last_seen: uint32 unix seconds]`.

___

## Self-Hosting

Edge can be deployed in multiple ways. For security and TLS termination, we recommend using a reverse proxy (e.g.,
Nginx). All methods require Synthient firehose, a MaxMind-compatible `.mmdb` database, and an Edge configuration file.

| Method                 | Notes                                                                                   |
|------------------------|-----------------------------------------------------------------------------------------|
| **Docker Compose**     | Quick setup. May incur performance overhead.                                            |
| **Pre-built binaries** | Self-contained executables. See [releases](https://github.com/synthient/edge/releases). |
| **Build from source**  | Build with .NET 10 SDK. Run via `dotnet Edge.dll`                                       |

---

## License & Third-Party Dependencies

Edge is licensed under BUSL-1.1 and includes third-party dependencies covered by their
respective [licenses](THIRD_PARTY_NOTICES.txt).

**Dependencies:**

| Library / Component | Version | License    |
|---------------------|---------|------------|
| MaxMind.Db          | v4.3.4  | Apache-2.0 |
| StackExchange.Redis | v2.11.8 | MIT        |
| YamlDotNet          | v16.3.0 | MIT        |
| AsyncKeyedLock      | v8.0.2  | MIT        |

___

> [!NOTE]
> Edge is currently in early alpha. Features and APIs are under active development. For bugs or questions, please open
> an issue or reach out through official channels.
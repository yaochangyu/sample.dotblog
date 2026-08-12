# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this repo is

A lab project demonstrating how to correctly handle a large (~1MB) strongly-typed array in an ASP.NET Core JSON request body without letting each request dump a fresh Large Object Heap (LOH, ≥85,000 bytes) allocation on the GC. It's a minimal API with two endpoints applying the same ArrayPool pattern (one over a primitive `double[]`, one over a nested domain struct) plus the scripts used to reproduce and observe the LOH behavior the pattern is designed to avoid.

## Commands

All commands run from the repo root (`Lab.LargeObject.slnx`), targeting `net10.0`.

```bash
dotnet build                      # build the whole solution
dotnet test                       # run all tests
dotnet test --filter "FullyQualifiedName~Post_Readings_接收超過LOH門檻的大陣列"   # run a single test
dotnet run --project src/Lab.LargeObject.Api   # run the API (default: http://localhost:5138)
```

### Load-testing / observing LOH behavior

`scripts/` holds manual diagnostic tooling (not part of `dotnet test`), meant to be run against a live instance in two terminals:

```bash
# terminal 1: run the API
ASPNETCORE_URLS=http://localhost:5080 dotnet run --project src/Lab.LargeObject.Api

# terminal 2: observe GC/LOH counters while under load (needs: dotnet tool install -g dotnet-counters)
./scripts/observe-counters.sh Lab.LargeObject.Api 60

# terminal 3: generate concurrent load with a ~1MB payload (needs: curl, awk, xargs)
./scripts/load-test.sh http://localhost:5080 /api/readings 20 500
```

`observe-counters.sh` wraps `dotnet-counters collect`, polling `System.Runtime` counters (`dotnet.gc.last_collection.heap.size` broken out by generation including `loh`, `dotnet.gc.last_collection.heap.fragmentation.size`, `dotnet.gc.collections`, `dotnet.gc.heap.total_allocated`, `dotnet.process.memory.working_set`) into a timestamped CSV in `scripts/`. `load-test.sh` generates and caches a 131,072-element `double` JSON payload (`payload-1mb.json`, ~1MB — deliberately over the LOH threshold) and fires it concurrently via `xargs -P`.

## Architecture

**`src/Lab.LargeObject.Api/Program.cs`** — minimal API, two endpoints: `POST /api/readings` (`PooledArray<double>`) and `POST /api/members` (`PooledArray<MemberAccount>`, a nested domain struct). Top-level statements end with `public partial class Program;` so `WebApplicationFactory<Program>` in the test project can reference the entry point.

**The core pattern (`PooledArray.cs` + one `JsonConverter<PooledArray<T>>` per element type)** — solves LOH pressure from repeated large-array deserialization:

- Each converter (`PooledDoubleArrayJsonConverter`, `PooledMemberAccountArrayJsonConverter`) is registered globally via `ConfigureHttpJsonOptions` in `Program.cs`. Instead of letting `System.Text.Json` allocate a fresh `new T[]` per request (which lands directly on the LOH and becomes garbage the moment the request ends), it reads the JSON array manually and rents its backing buffer from `ArrayPool<T>.Shared`, doubling and swapping to a larger rented buffer as needed while consuming the array.
- `PooledArray<T>` is a `readonly struct` wrapping the rented array plus the *actual* used `Length` (rented arrays are oversized — never trust `array.Length` from a pool rental) and a `Span` view over just the used portion. It implements `IDisposable`; `Dispose()` returns the array to the pool (clearing it first if `T` contains references, so pooled memory doesn't keep stale object graphs alive).
- Endpoint handlers wrap all use of the bound `PooledArray<T>` in `using (x) { ... }`, keeping the rented buffer's lifetime strictly scoped to the request so it's returned to the pool before the response completes. **Never let a `PooledArray<T>` escape this scope** — anything holding a reference to it after `Dispose()` runs is a use-after-return bug, since the pool can hand the same array to a concurrent request.
- Each converter is written for one specific element type. Adding another array-typed endpoint means writing an analogous converter (or generalizing) rather than binding a plain `[FromBody] T[]` parameter directly — that bypasses the pool entirely and reintroduces the LOH allocation this pattern exists to avoid.
- **`PooledMemberAccountArrayJsonConverter` is the template to copy for nested/complex element types**: it only pools the *array container* (`ArrayPool<MemberAccount>`); each element's nested fields (`ContactInfo`, etc.) are deserialized by delegating to `JsonSerializer.Deserialize<MemberAccount>(ref reader, options)` for that one array slot, letting System.Text.Json recurse normally. Don't hand-parse nested object fields token-by-token — only the outer array allocation is the LOH concern.
- `MemberAccount`/`ContactInfo` (`MemberAccount.cs`) are deliberately `readonly struct`, not `class`. A `class` element type means the array only holds references (pointers) — individually `new`'d objects scattered on the heap that `ArrayPool<T>` on the array does nothing to avoid. Structs are stored inline in the array's own contiguous block, which is what makes pooling the array meaningful.

**`tests/Lab.LargeObject.Api.Tests/`** — integration tests via `WebApplicationFactory<Program>` + real `HttpClient`, not unit tests of converter internals. `LargeArrayEndpointTests.cs` posts 131,072 doubles (~1MB); `MemberAccountEndpointTests.cs` posts 20,000 nested `MemberAccount` objects — both sizes chosen specifically to cross the 85,000-byte LOH threshold on the array container. Each covers the happy path (counts/aggregates round-trip correctly) plus an empty-array edge case. Test method names are Traditional Chinese, Given-When-Then-style (e.g. `Post_Readings_接收超過LOH門檻的大陣列_回傳正確統計結果`) — follow this convention for new tests in this project.

# Docker Setup Guide

This was tested using Linux. This provides an alternative to building and using the project directly on your system.

This currently only supports SQLite for the database.

## Clone the Repo

First, clone the git repository like normal, Then make sure to go into the directory.

## Touch DB Files

This step is needed to provide empty database files to mount for the first launch, due to the way docker mounts handle missing files. This will only need to be done once before starting fresh.

```bash
touch rasaauth.db
touch rasachar.db
touch rasaworld.db
```

## Create App Settings

Next, create a appsettings.env.json in the root directory with the following contents, replacing the ip address with the one that is running the docker containers. This is useful especially when the system you're running the game on is different from where the containers are running.

```json
{
  "CommunicatorConfig": {
    "Address": "192.168.0.26"
  },
  "GameConfig": {
    "PublicAddress": "192.168.0.26"
  }
}
```

## Start Server

Next, run `docker compose up`

## Create a User

Like the setup docs mention, the next step is creating a user. To do so, attach to the auth server and run the command.

## Play the Game

Now, you should be able to run `tabula_rasa.exe /NoPatch /AuthServer=192.168.0.26:2106` and login.

## Runtime image (Release build, since 2026-09-27)

The `Dockerfile` has two stages. The `mcr.microsoft.com/dotnet/sdk:5.0` stage runs `dotnet publish -c Release` for
Game and Auth; the final image is `mcr.microsoft.com/dotnet/runtime:5.0` with only the published output. Both
services use the same image, and `docker-compose.yml` starts them with `dotnet src/Rasa.Game/Rasa.Game.dll` and
`dotnet src/Rasa.Auth/Rasa.Auth.dll`. Before this change both ran a Debug build through
`dotnet run --project ...` in the SDK image, which rebuilt the project at every container start and left the
`dotnet run` host and MSBuild worker processes resident.

The paths the server resolves are unchanged, so the compose mounts (and the DIT overlay's `./dit:/app/dit`) are the
same:

- Working directory `/app`: `rasaauth.db`, `rasachar.db`, `rasaworld.db`, `navmesh/` (`NavMeshPath`),
  `kb-articles.json` (`KnowledgeBaseFile`), `log-game.txt` / `log-auth.txt`, and `/app/dit`.
- `AppContext.BaseDirectory`, now `/app/src/Rasa.Game` and `/app/src/Rasa.Auth` (it was `.../bin/Debug/net5.0`):
  `appsettings.json`, `appsettings.env.json`, `databasesettings.json` and `Content/Cover/*.cover.json`.

One difference: `./appsettings.env.json` is mounted at `/app/src/Rasa.Game/appsettings.env.json`, which is now
the file the server reads. Under `dotnet run` the build copied it into `bin/Debug/net5.0` at container start, so
`reload config` only saw the copy. Now `reload config` reads the mounted file, so an in-place edit takes effect
without a restart. An editor that replaces the file breaks the single-file bind mount; restart the container then.

`docs/evidence` is no longer in the image. Only `Rasa.Test` reads it, and tests run in the SDK image with the
checkout mounted (for example `docker run --rm -v "$PWD":/src -w /src mcr.microsoft.com/dotnet/sdk:5.0 dotnet test
src/Rasa.Test`). The runtime image cannot run tests, so recipes that ran `dotnet test` inside `rasa_net` no longer
work. `global.json` is in `.dockerignore`, so a checkout that pins a newer SDK still builds with .NET 5. Stay on
.NET 5: rolling forward breaks the EF Core 5 queries.

`docker-compose.yml` caps memory at 4 GB for Game (live Debug reached 2.1 GB after hours of play; the guardrail rule is at least current use plus 1 GB) and 512 MB for Auth (`mem_limit`). If a process hits the cap,
the container is killed and restarted by `restart: always`, instead of pushing the host into swap. Game is
already on workstation GC (no `ServerGarbageCollection`), so `DOTNET_gcServer=0` changes nothing.
`DOTNET_GCConserveMemory` needs .NET 6 or later. `DOTNET_gcConcurrent=0` and `DOTNET_GCgen0size=0x1000000`
made no measurable difference (1.62 GiB against 1.54 GiB, within run-to-run noise), so neither is set.

Measured locally on 2026-09-27 (development 4895bc1, the deploy world database, navmesh mounted, empty char and
auth databases, about four minutes after start):

| | Debug `dotnet run` (SDK image) | Release (runtime image) |
|---|---|---|
| Image size | 3.75 GB (1.32 GB content) | 398 MB (104 MB content) |
| Game container (`docker stats`) | 1.62 GiB | 1.42 GiB |
| Game server process RSS | 1.53 GB, plus 96 MB `dotnet run` and 3 MSBuild nodes of 105-113 MB | 1.53 GB, nothing else |
| Auth container (`docker stats`) | 195 MiB | 42 MiB |
| Container start to "authenticated with the Auth server" | 31 s (includes the rebuild) | 25 s |

The startup logs are identical apart from IP addresses and timestamps: 160 world, 16 char and 2 auth migrations,
839 SpawnPools, navmeshes for 75 of 75 maps, 16 content rules (573 rows, 0 gaps), listening on 8102, authenticated
with Auth. The console commands (`perf`, `reload config`, ...) work through `docker attach` as before.

### Building the DIT image

There is no SDK in the runtime image, so the DIT overlay can no longer be compiled on top of the base image. Build
it from a source tree of the same commit with the overlay applied, using the repository `Dockerfile`:

```sh
mkdir dit-build && git archive <commit> | tar -x -C dit-build && cd dit-build
O=<overlay dir>   # src/Rasa.Game/Dit/, src/Rasa.Test/Dit*.cs, dit-overlay.patch
cp -a "$O/src/Rasa.Game/Dit" src/Rasa.Game/
cp "$O"/src/Rasa.Test/Dit*.cs src/Rasa.Test/
git apply --check "$O/dit-overlay.patch" && git apply "$O/dit-overlay.patch"
docker build -t rasa_net_game:dit-<tag> .
```

The DIT tests in `src/Rasa.Test` are not built into the image. Run them, and the full suite, in the SDK image as
above. `docker-compose.dit.yml` only overrides `image` and adds `./dit:/app/dit`, so it inherits the new command
from `docker-compose.yml`. A Debug-era image (anything built before this change, including the rollback tags)
has no published `Rasa.Game.dll` at that path. To roll back to one, restore the previous `docker-compose.yml`
(`command: dotnet run --project ...`) as well as the image pin.

## Deploy lock on banshee-ax41 (since 2026-09-28)

While a deploy is under way, `~/servers/rasa-net/.deploy-lock` exists. Create it before the backups and remove it
after Game is healthy. The on-demand sleep service (stackd) and the DIT offline-sim apply step check for it: stackd
does not stop Game while it exists, and offline-sim keeps its plan pending instead of writing to `rasachar.db`.

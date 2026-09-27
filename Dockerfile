# Two stages: the .NET 5 SDK publishes Release builds of Game and Auth, and only the published output goes into the
# much smaller .NET 5 runtime image. Both services use this one image (see docker-compose.yml for the commands).
#
# The published layout keeps every path the server resolves at the container paths the Debug `dotnet run` image
# used, so the compose mounts do not change:
# - the working directory stays /app: rasaauth.db, rasachar.db and rasaworld.db ("Data Source=<name>.db"),
#   navmesh (GameDataConfig.NavMeshPath), kb-articles.json (GameDataConfig.KnowledgeBaseFile), dit (the DIT
#   overlay) and the log-*.txt files are all relative to it;
# - each program is published into its project folder (/app/src/Rasa.Game, /app/src/Rasa.Auth), which is its
#   AppContext.BaseDirectory, where appsettings.json, appsettings.env.json, databasesettings.json and
#   Content/Cover/*.cover.json are read. The ./appsettings.env.json mount at /app/src/Rasa.Game/appsettings.env.json
#   therefore sits next to Rasa.Game.dll and is read directly.
# docs/evidence is read only by Rasa.Test, which is not part of this image (tests run in the SDK image).

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build

WORKDIR /app

COPY src /app/src
COPY Rasa.NET.sln /app
COPY Rasa.NET.sln.DotSettings /app

RUN dotnet restore src/Rasa.Game/Rasa.Game.csproj \
 && dotnet restore src/Rasa.Auth/Rasa.Auth.csproj
RUN dotnet publish src/Rasa.Game/Rasa.Game.csproj -c Release --no-restore -o /publish/Rasa.Game \
 && dotnet publish src/Rasa.Auth/Rasa.Auth.csproj -c Release --no-restore -o /publish/Rasa.Auth

FROM mcr.microsoft.com/dotnet/runtime:5.0

WORKDIR /app

COPY --from=build /publish/Rasa.Game /app/src/Rasa.Game
COPY --from=build /publish/Rasa.Auth /app/src/Rasa.Auth

# Without a command the image runs Game; docker-compose.yml sets the command for each service.
CMD ["dotnet", "/app/src/Rasa.Game/Rasa.Game.dll"]

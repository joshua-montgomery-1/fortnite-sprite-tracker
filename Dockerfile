FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore FortniteSpriteTracker.sln
RUN dotnet publish src/FortniteSpriteTracker.Server/FortniteSpriteTracker.Server.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

LABEL org.opencontainers.image.source="https://github.com/joshua-montgomery-1/fortnite-sprite-tracker"
LABEL org.opencontainers.image.title="Fortnite Sprite Tracker"
LABEL org.opencontainers.image.description="Sprite Scout hosted Blazor WebAssembly application"

ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "FortniteSpriteTracker.Server.dll"]

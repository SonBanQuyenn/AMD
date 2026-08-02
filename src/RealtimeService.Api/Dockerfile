FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Shared.Contracts/Shared.Contracts.csproj Shared.Contracts/
COPY src/RealtimeService.Api/RealtimeService.Api.csproj RealtimeService.Api/
RUN dotnet restore RealtimeService.Api/RealtimeService.Api.csproj

COPY src/Shared.Contracts/ Shared.Contracts/
COPY src/RealtimeService.Api/ RealtimeService.Api/

WORKDIR /src/RealtimeService.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "RealtimeService.Api.dll"]

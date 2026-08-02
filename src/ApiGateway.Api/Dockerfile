FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/ApiGateway.Api/ApiGateway.Api.csproj ApiGateway.Api/
RUN dotnet restore ApiGateway.Api/ApiGateway.Api.csproj

COPY src/ApiGateway.Api/ ApiGateway.Api/

WORKDIR /src/ApiGateway.Api
RUN dotnet publish -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "ApiGateway.Api.dll"]

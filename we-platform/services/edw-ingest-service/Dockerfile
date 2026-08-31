FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/events/WePlatform.Events.csproj shared/events/
COPY services/edw-ingest-service/src/Domain/EdwIngestService.Domain.csproj services/edw-ingest-service/src/Domain/
COPY services/edw-ingest-service/src/Application/EdwIngestService.Application.csproj services/edw-ingest-service/src/Application/
COPY services/edw-ingest-service/src/Infrastructure/EdwIngestService.Infrastructure.csproj services/edw-ingest-service/src/Infrastructure/
COPY services/edw-ingest-service/src/Api/EdwIngestService.Api.csproj services/edw-ingest-service/src/Api/
RUN dotnet restore services/edw-ingest-service/src/Api/EdwIngestService.Api.csproj
COPY shared/events/ shared/events/
COPY services/edw-ingest-service/src/ services/edw-ingest-service/src/
RUN dotnet publish services/edw-ingest-service/src/Api/EdwIngestService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EdwIngestService.Api.dll"]

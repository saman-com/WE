FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/events/WePlatform.Events.csproj shared/events/
COPY services/evidence-service/src/Domain/EvidenceService.Domain.csproj services/evidence-service/src/Domain/
COPY services/evidence-service/src/Application/EvidenceService.Application.csproj services/evidence-service/src/Application/
COPY services/evidence-service/src/Infrastructure/EvidenceService.Infrastructure.csproj services/evidence-service/src/Infrastructure/
COPY services/evidence-service/src/Api/EvidenceService.Api.csproj services/evidence-service/src/Api/
RUN dotnet restore services/evidence-service/src/Api/EvidenceService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY shared/events/ shared/events/
COPY services/evidence-service/src/ services/evidence-service/src/
RUN dotnet publish services/evidence-service/src/Api/EvidenceService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EvidenceService.Api.dll"]

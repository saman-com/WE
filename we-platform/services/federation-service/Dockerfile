FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/federation-service/src/Domain/FederationService.Domain.csproj services/federation-service/src/Domain/
COPY services/federation-service/src/Application/FederationService.Application.csproj services/federation-service/src/Application/
COPY services/federation-service/src/Infrastructure/FederationService.Infrastructure.csproj services/federation-service/src/Infrastructure/
COPY services/federation-service/src/Api/FederationService.Api.csproj services/federation-service/src/Api/
RUN dotnet restore services/federation-service/src/Api/FederationService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY shared/aspnet/ shared/aspnet/
COPY services/federation-service/src/ services/federation-service/src/
RUN dotnet publish services/federation-service/src/Api/FederationService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "FederationService.Api.dll"]

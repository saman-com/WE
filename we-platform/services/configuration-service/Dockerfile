FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY services/configuration-service/src/Domain/ConfigurationService.Domain.csproj services/configuration-service/src/Domain/
COPY services/configuration-service/src/Application/ConfigurationService.Application.csproj services/configuration-service/src/Application/
COPY services/configuration-service/src/Infrastructure/ConfigurationService.Infrastructure.csproj services/configuration-service/src/Infrastructure/
COPY services/configuration-service/src/Api/ConfigurationService.Api.csproj services/configuration-service/src/Api/
RUN dotnet restore services/configuration-service/src/Api/ConfigurationService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY services/configuration-service/src/ services/configuration-service/src/
RUN dotnet publish services/configuration-service/src/Api/ConfigurationService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ConfigurationService.Api.dll"]

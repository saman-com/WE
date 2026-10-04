FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY services/organisation-service/src/Domain/OrganisationService.Domain.csproj services/organisation-service/src/Domain/
COPY services/organisation-service/src/Application/OrganisationService.Application.csproj services/organisation-service/src/Application/
COPY services/organisation-service/src/Infrastructure/OrganisationService.Infrastructure.csproj services/organisation-service/src/Infrastructure/
COPY services/organisation-service/src/Api/OrganisationService.Api.csproj services/organisation-service/src/Api/
RUN dotnet restore services/organisation-service/src/Api/OrganisationService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY services/organisation-service/src/ services/organisation-service/src/
RUN dotnet publish services/organisation-service/src/Api/OrganisationService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrganisationService.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/intervention-service/src/Domain/InterventionService.Domain.csproj services/intervention-service/src/Domain/
COPY services/intervention-service/src/Application/InterventionService.Application.csproj services/intervention-service/src/Application/
COPY services/intervention-service/src/Infrastructure/InterventionService.Infrastructure.csproj services/intervention-service/src/Infrastructure/
COPY services/intervention-service/src/Api/InterventionService.Api.csproj services/intervention-service/src/Api/
RUN dotnet restore services/intervention-service/src/Api/InterventionService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY shared/aspnet/ shared/aspnet/
COPY services/intervention-service/src/ services/intervention-service/src/
RUN dotnet publish services/intervention-service/src/Api/InterventionService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "InterventionService.Api.dll"]

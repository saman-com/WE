FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/identity-service/src/Domain/IdentityService.Domain.csproj services/identity-service/src/Domain/
COPY services/identity-service/src/Application/IdentityService.Application.csproj services/identity-service/src/Application/
COPY services/identity-service/src/Infrastructure/IdentityService.Infrastructure.csproj services/identity-service/src/Infrastructure/
COPY services/identity-service/src/Api/IdentityService.Api.csproj services/identity-service/src/Api/
RUN dotnet restore services/identity-service/src/Api/IdentityService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY shared/aspnet/ shared/aspnet/
COPY services/identity-service/src/ services/identity-service/src/
RUN dotnet publish services/identity-service/src/Api/IdentityService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/ei-service/src/Domain/EiService.Domain.csproj services/ei-service/src/Domain/
COPY services/ei-service/src/Application/EiService.Application.csproj services/ei-service/src/Application/
COPY services/ei-service/src/Infrastructure/EiService.Infrastructure.csproj services/ei-service/src/Infrastructure/
COPY services/ei-service/src/Api/EiService.Api.csproj services/ei-service/src/Api/
RUN dotnet restore services/ei-service/src/Api/EiService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY shared/aspnet/ shared/aspnet/
COPY services/ei-service/src/ services/ei-service/src/
RUN dotnet publish services/ei-service/src/Api/EiService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EiService.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/reporting-service/src/Domain/ReportingService.Domain.csproj services/reporting-service/src/Domain/
COPY services/reporting-service/src/Application/ReportingService.Application.csproj services/reporting-service/src/Application/
COPY services/reporting-service/src/Infrastructure/ReportingService.Infrastructure.csproj services/reporting-service/src/Infrastructure/
COPY services/reporting-service/src/Api/ReportingService.Api.csproj services/reporting-service/src/Api/
RUN dotnet restore services/reporting-service/src/Api/ReportingService.Api.csproj
COPY services/reporting-service/src/ services/reporting-service/src/
RUN dotnet publish services/reporting-service/src/Api/ReportingService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "ReportingService.Api.dll"]

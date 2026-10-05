FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/national-reporting-service/src/Domain/NationalReportingService.Domain.csproj services/national-reporting-service/src/Domain/
COPY services/national-reporting-service/src/Application/NationalReportingService.Application.csproj services/national-reporting-service/src/Application/
COPY services/national-reporting-service/src/Infrastructure/NationalReportingService.Infrastructure.csproj services/national-reporting-service/src/Infrastructure/
COPY services/national-reporting-service/src/Api/NationalReportingService.Api.csproj services/national-reporting-service/src/Api/
RUN dotnet restore services/national-reporting-service/src/Api/NationalReportingService.Api.csproj
COPY shared/aspnet/ shared/aspnet/
COPY services/national-reporting-service/src/ services/national-reporting-service/src/
RUN dotnet publish services/national-reporting-service/src/Api/NationalReportingService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NationalReportingService.Api.dll"]

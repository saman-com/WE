FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/events/WePlatform.Events.csproj shared/events/
COPY shared/messaging/WePlatform.Messaging.csproj shared/messaging/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/diagnostic-service/src/Domain/DiagnosticService.Domain.csproj services/diagnostic-service/src/Domain/
COPY services/diagnostic-service/src/Application/DiagnosticService.Application.csproj services/diagnostic-service/src/Application/
COPY services/diagnostic-service/src/Infrastructure/DiagnosticService.Infrastructure.csproj services/diagnostic-service/src/Infrastructure/
COPY services/diagnostic-service/src/Api/DiagnosticService.Api.csproj services/diagnostic-service/src/Api/
RUN dotnet restore services/diagnostic-service/src/Api/DiagnosticService.Api.csproj
COPY shared/events/ shared/events/
COPY shared/tenancy/ shared/tenancy/
COPY shared/messaging/ shared/messaging/
COPY shared/aspnet/ shared/aspnet/
COPY services/diagnostic-service/src/ services/diagnostic-service/src/
RUN dotnet publish services/diagnostic-service/src/Api/DiagnosticService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "DiagnosticService.Api.dll"]

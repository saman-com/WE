FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/events/WePlatform.Events.csproj shared/events/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/notification-service/src/Domain/NotificationService.Domain.csproj services/notification-service/src/Domain/
COPY services/notification-service/src/Application/NotificationService.Application.csproj services/notification-service/src/Application/
COPY services/notification-service/src/Infrastructure/NotificationService.Infrastructure.csproj services/notification-service/src/Infrastructure/
COPY services/notification-service/src/Api/NotificationService.Api.csproj services/notification-service/src/Api/
RUN dotnet restore services/notification-service/src/Api/NotificationService.Api.csproj
COPY shared/events/ shared/events/
COPY shared/tenancy/ shared/tenancy/
COPY shared/aspnet/ shared/aspnet/
COPY services/notification-service/src/ services/notification-service/src/
RUN dotnet publish services/notification-service/src/Api/NotificationService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "NotificationService.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/events/WePlatform.Events.csproj shared/events/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/event-subscriber-service/src/Api/EventSubscriberService.Api.csproj services/event-subscriber-service/src/Api/
RUN dotnet restore services/event-subscriber-service/src/Api/EventSubscriberService.Api.csproj
COPY shared/events/ shared/events/
COPY shared/aspnet/ shared/aspnet/
COPY services/event-subscriber-service/src/ services/event-subscriber-service/src/
RUN dotnet publish services/event-subscriber-service/src/Api/EventSubscriberService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends curl \
  && rm -rf /var/lib/apt/lists/*
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EventSubscriberService.Api.dll"]

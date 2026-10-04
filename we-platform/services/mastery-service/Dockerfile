FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/events/WePlatform.Events.csproj shared/events/
COPY services/mastery-service/src/Domain/MasteryService.Domain.csproj services/mastery-service/src/Domain/
COPY services/mastery-service/src/Application/MasteryService.Application.csproj services/mastery-service/src/Application/
COPY services/mastery-service/src/Infrastructure/MasteryService.Infrastructure.csproj services/mastery-service/src/Infrastructure/
COPY services/mastery-service/src/Api/MasteryService.Api.csproj services/mastery-service/src/Api/
RUN dotnet restore services/mastery-service/src/Api/MasteryService.Api.csproj
COPY shared/events/ shared/events/
COPY shared/tenancy/ shared/tenancy/
COPY services/mastery-service/src/ services/mastery-service/src/
RUN dotnet publish services/mastery-service/src/Api/MasteryService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MasteryService.Api.dll"]

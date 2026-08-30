FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/communication-service/src/Domain/CommunicationService.Domain.csproj services/communication-service/src/Domain/
COPY services/communication-service/src/Application/CommunicationService.Application.csproj services/communication-service/src/Application/
COPY services/communication-service/src/Infrastructure/CommunicationService.Infrastructure.csproj services/communication-service/src/Infrastructure/
COPY services/communication-service/src/Api/CommunicationService.Api.csproj services/communication-service/src/Api/
RUN dotnet restore services/communication-service/src/Api/CommunicationService.Api.csproj
COPY services/communication-service/src/ services/communication-service/src/
RUN dotnet publish services/communication-service/src/Api/CommunicationService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CommunicationService.Api.dll"]

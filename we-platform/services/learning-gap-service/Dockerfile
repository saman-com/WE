FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/events/WePlatform.Events.csproj shared/events/
COPY services/learning-gap-service/src/Domain/LearningGapService.Domain.csproj services/learning-gap-service/src/Domain/
COPY services/learning-gap-service/src/Application/LearningGapService.Application.csproj services/learning-gap-service/src/Application/
COPY services/learning-gap-service/src/Infrastructure/LearningGapService.Infrastructure.csproj services/learning-gap-service/src/Infrastructure/
COPY services/learning-gap-service/src/Api/LearningGapService.Api.csproj services/learning-gap-service/src/Api/
RUN dotnet restore services/learning-gap-service/src/Api/LearningGapService.Api.csproj
COPY shared/events/ shared/events/
COPY shared/tenancy/ shared/tenancy/
COPY services/learning-gap-service/src/ services/learning-gap-service/src/
RUN dotnet publish services/learning-gap-service/src/Api/LearningGapService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "LearningGapService.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/events/WePlatform.Events.csproj shared/events/
COPY shared/messaging/WePlatform.Messaging.csproj shared/messaging/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/student-learning-service/src/Domain/StudentLearningService.Domain.csproj services/student-learning-service/src/Domain/
COPY services/student-learning-service/src/Application/StudentLearningService.Application.csproj services/student-learning-service/src/Application/
COPY services/student-learning-service/src/Infrastructure/StudentLearningService.Infrastructure.csproj services/student-learning-service/src/Infrastructure/
COPY services/student-learning-service/src/Api/StudentLearningService.Api.csproj services/student-learning-service/src/Api/
RUN dotnet restore services/student-learning-service/src/Api/StudentLearningService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY shared/events/ shared/events/
COPY shared/messaging/ shared/messaging/
COPY shared/aspnet/ shared/aspnet/
COPY services/student-learning-service/src/ services/student-learning-service/src/
RUN dotnet publish services/student-learning-service/src/Api/StudentLearningService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "StudentLearningService.Api.dll"]

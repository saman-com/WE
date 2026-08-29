FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/assessment-service/src/Domain/AssessmentService.Domain.csproj services/assessment-service/src/Domain/
COPY services/assessment-service/src/Application/AssessmentService.Application.csproj services/assessment-service/src/Application/
COPY services/assessment-service/src/Infrastructure/AssessmentService.Infrastructure.csproj services/assessment-service/src/Infrastructure/
COPY services/assessment-service/src/Api/AssessmentService.Api.csproj services/assessment-service/src/Api/
RUN dotnet restore services/assessment-service/src/Api/AssessmentService.Api.csproj
COPY services/assessment-service/src/ services/assessment-service/src/
RUN dotnet publish services/assessment-service/src/Api/AssessmentService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AssessmentService.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/tenancy/WePlatform.Tenancy.csproj shared/tenancy/
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY services/curriculum-service/src/Domain/CurriculumService.Domain.csproj services/curriculum-service/src/Domain/
COPY services/curriculum-service/src/Application/CurriculumService.Application.csproj services/curriculum-service/src/Application/
COPY services/curriculum-service/src/Infrastructure/CurriculumService.Infrastructure.csproj services/curriculum-service/src/Infrastructure/
COPY services/curriculum-service/src/Api/CurriculumService.Api.csproj services/curriculum-service/src/Api/
RUN dotnet restore services/curriculum-service/src/Api/CurriculumService.Api.csproj
COPY shared/tenancy/ shared/tenancy/
COPY shared/aspnet/ shared/aspnet/
COPY services/curriculum-service/src/ services/curriculum-service/src/
RUN dotnet publish services/curriculum-service/src/Api/CurriculumService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "CurriculumService.Api.dll"]

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY shared/aspnet/WePlatform.AspNetCore.csproj shared/aspnet/
COPY examples/sample-service/src/Api/SampleService.Api.csproj examples/sample-service/src/Api/
RUN dotnet restore examples/sample-service/src/Api/SampleService.Api.csproj
COPY shared/aspnet/ shared/aspnet/
COPY examples/sample-service/src/ examples/sample-service/src/
RUN dotnet publish examples/sample-service/src/Api/SampleService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SampleService.Api.dll"]

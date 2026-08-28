FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/Api/SampleService.Api.csproj src/Api/
RUN dotnet restore src/Api/SampleService.Api.csproj
COPY src/Api/ src/Api/
RUN dotnet publish src/Api/SampleService.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SampleService.Api.dll"]

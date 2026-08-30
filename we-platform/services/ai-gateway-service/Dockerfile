FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY services/ai-gateway-service/src/Domain/AiGatewayService.Domain.csproj services/ai-gateway-service/src/Domain/
COPY services/ai-gateway-service/src/Application/AiGatewayService.Application.csproj services/ai-gateway-service/src/Application/
COPY services/ai-gateway-service/src/Infrastructure/AiGatewayService.Infrastructure.csproj services/ai-gateway-service/src/Infrastructure/
COPY services/ai-gateway-service/src/Api/AiGatewayService.Api.csproj services/ai-gateway-service/src/Api/
RUN dotnet restore services/ai-gateway-service/src/Api/AiGatewayService.Api.csproj
COPY services/ai-gateway-service/src/ services/ai-gateway-service/src/
COPY ai/prompts/ ai/prompts/
RUN dotnet publish services/ai-gateway-service/src/Api/AiGatewayService.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV Prompts__RootPath=/app/prompts
COPY --from=build /app/publish .
COPY --from=build /src/ai/prompts /app/prompts
ENTRYPOINT ["dotnet", "AiGatewayService.Api.dll"]

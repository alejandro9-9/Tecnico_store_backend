FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TecFast.Api/ ./TecFast.Api/
COPY TecFast.Domain/ ./TecFast.Domain/
COPY TecFast.Application/ ./TecFast.Application/
COPY TecFast.Infrastructure/ ./TecFast.Infrastructure/
COPY catalog.csv known_models.csv model_repair_prices.csv symptom_rules.csv device_rules.csv ./
RUN dotnet publish TecFast.Api/TecFast.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000 10000
ENTRYPOINT ["sh", "-c", "dotnet TecFast.Api.dll --urls http://0.0.0.0:${PORT:-8000}"]

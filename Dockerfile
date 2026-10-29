FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Evol.Api/ ./Evol.Api/
COPY Evol.Domain/ ./Evol.Domain/
COPY Evol.Application/ ./Evol.Application/
COPY Evol.Infrastructure/ ./Evol.Infrastructure/
COPY catalog.csv known_models.csv model_repair_prices.csv symptom_rules.csv ./
RUN dotnet publish Evol.Api/Evol.Api.csproj -c Release -o /out

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out ./
ENV ASPNETCORE_URLS=http://+:8000
EXPOSE 8000 10000
ENTRYPOINT ["sh", "-c", "dotnet Evol.Api.dll --urls http://0.0.0.0:${PORT:-8000}"]

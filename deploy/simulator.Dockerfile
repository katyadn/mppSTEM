# Публікує Worker; фінальний runtime-образ запускає його без SDK. Поточний Worker лише журналює час.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Microclimate.SensorSimulator/Microclimate.SensorSimulator.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /out .
# Непривілейований користувач стандартного .NET-образу.
USER $APP_UID
ENTRYPOINT ["dotnet", "Microclimate.SensorSimulator.dll"]

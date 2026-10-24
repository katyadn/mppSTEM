# Публікує API в SDK-образі; фінальний aspnet-образ містить лише runtime та результат publish.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish src/Microclimate.Api/Microclimate.Api.csproj -c Release -o /out /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /out .
# Непривілейований користувач стандартного .NET-образу.
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Microclimate.Api.dll"]

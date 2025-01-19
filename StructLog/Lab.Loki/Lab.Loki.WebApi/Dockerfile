FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lab.Loki.WebApi/Lab.Loki.WebApi.csproj", "Lab.Loki.WebApi/"]
RUN dotnet restore "Lab.Loki.WebApi/Lab.Loki.WebApi.csproj"
COPY . .
WORKDIR "/src/Lab.Loki.WebApi"
RUN dotnet build "Lab.Loki.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Lab.Loki.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab.Loki.WebApi.dll"]

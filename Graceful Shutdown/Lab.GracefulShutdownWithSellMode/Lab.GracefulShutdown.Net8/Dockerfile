#FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
FROM mcr.microsoft.com/dotnet/runtime:8.0-alpine AS base

WORKDIR /app

#FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build

ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lab.GracefulShutdown.Net8/Lab.GracefulShutdown.Net8.csproj", "Lab.GracefulShutdown.Net8/"]
RUN dotnet restore "Lab.GracefulShutdown.Net8/Lab.GracefulShutdown.Net8.csproj"
COPY . .
WORKDIR "/src/Lab.GracefulShutdown.Net8"
RUN dotnet build "Lab.GracefulShutdown.Net8.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Lab.GracefulShutdown.Net8.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
#ENTRYPOINT ["dotnet", "Lab.GracefulShutdown.Net8.dll"]
#CMD ["dotnet","Lab.GracefulShutdown.Net8.dll"] #exec mode
CMD dotnet /app/Lab.GracefulShutdown.Net8.dll #shell mode
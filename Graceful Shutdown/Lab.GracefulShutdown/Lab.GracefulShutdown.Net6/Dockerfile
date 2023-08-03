FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Lab.GracefulShutdown.Net6/Lab.GracefulShutdown.Net6.csproj", "Lab.GracefulShutdown.Net6/"]
RUN dotnet restore "Lab.GracefulShutdown.Net6/Lab.GracefulShutdown.Net6.csproj"
COPY . .
WORKDIR "/src/Lab.GracefulShutdown.Net6"
RUN dotnet build "Lab.GracefulShutdown.Net6.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lab.GracefulShutdown.Net6.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab.GracefulShutdown.Net6.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Lab.TestContainers.WebApi/Lab.TestContainers.WebApi.csproj", "Lab.TestContainers.WebApi/"]
RUN dotnet restore "Lab.TestContainers.WebApi/Lab.TestContainers.WebApi.csproj"
COPY . .
WORKDIR "/src/Lab.TestContainers.WebApi"
RUN dotnet build "Lab.TestContainers.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lab.TestContainers.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab.TestContainers.WebApi.dll"]

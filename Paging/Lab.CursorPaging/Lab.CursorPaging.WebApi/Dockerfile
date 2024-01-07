FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Lab.CursorPaging.WebApi/Lab.CursorPaging.WebApi.csproj", "Lab.CursorPaging.WebApi/"]
RUN dotnet restore "Lab.CursorPaging.WebApi/Lab.CursorPaging.WebApi.csproj"
COPY . .
WORKDIR "/src/Lab.CursorPaging.WebApi"
RUN dotnet build "Lab.CursorPaging.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Lab.CursorPaging.WebApi.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab.CursorPaging.WebApi.dll"]

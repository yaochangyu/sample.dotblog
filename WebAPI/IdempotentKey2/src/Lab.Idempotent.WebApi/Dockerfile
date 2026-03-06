FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/Lab.Idempotent.WebApi/Lab.Idempotent.WebApi.csproj", "src/Lab.Idempotent.WebApi/"]
RUN dotnet restore "src/Lab.Idempotent.WebApi/Lab.Idempotent.WebApi.csproj"
COPY . .
WORKDIR "/src/src/Lab.Idempotent.WebApi"
RUN dotnet build "Lab.Idempotent.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lab.Idempotent.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab.Idempotent.WebApi.dll"]

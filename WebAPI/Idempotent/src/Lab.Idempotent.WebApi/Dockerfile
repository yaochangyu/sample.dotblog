FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Lab.Idempotent.WebApi/Lab.Idempotent.WebApi.csproj", "Lab.Idempotent.WebApi/"]
RUN dotnet restore "src/Lab.Idempotent.WebApi/Lab.Idempotent.WebApi.csproj"
COPY . .
WORKDIR "/src/Lab.Idempotent.WebApi"
RUN dotnet build "Lab.Idempotent.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lab.Idempotent.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab.Idempotent.WebApi.dll"]

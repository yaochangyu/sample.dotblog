FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["src/Lab.SpecFirst.Web/Lab.SpecFirst.Web.csproj", "Lab.SpecFirst.Web/"]
RUN dotnet restore "src/Lab.SpecFirst.Web/Lab.SpecFirst.Web.csproj"
COPY . .
WORKDIR "/src/Lab.SpecFirst.Web"
RUN dotnet build "Lab.SpecFirst.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Lab.SpecFirst.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Lab.SpecFirst.Web.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS app-build
WORKDIR /src
COPY ["Implementacija/Autosalon OneZone/Autosalon OneZone/Autosalon OneZone.csproj", "Implementacija/Autosalon OneZone/Autosalon OneZone/"]
RUN dotnet restore "Implementacija/Autosalon OneZone/Autosalon OneZone/Autosalon OneZone.csproj"
COPY . .

FROM app-build AS publish
RUN dotnet publish "Implementacija/Autosalon OneZone/Autosalon OneZone/Autosalon OneZone.csproj" \
    -c Release \
    --no-restore \
    -o /app/publish \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS test
WORKDIR /src
COPY ["Implementacija/Autosalon OneZone/Autosalon OneZone/Autosalon OneZone.csproj", "Implementacija/Autosalon OneZone/Autosalon OneZone/"]
COPY ["Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/AutosalonOneZone.Tests.csproj", "Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/"]
RUN dotnet restore "Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/AutosalonOneZone.Tests.csproj"
COPY . .
RUN dotnet test "Implementacija/Autosalon OneZone/AutosalonOneZone.Tests/AutosalonOneZone.Tests.csproj" \
    -c Release \
    --no-restore \
    --logger "console;verbosity=normal"

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Autosalon OneZone.dll"]

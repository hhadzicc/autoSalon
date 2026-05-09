FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["Implementacija/Autosalon OneZone/Autosalon OneZone/Autosalon OneZone.csproj", "Implementacija/Autosalon OneZone/Autosalon OneZone/"]
RUN dotnet restore "Implementacija/Autosalon OneZone/Autosalon OneZone/Autosalon OneZone.csproj"
COPY . .
RUN dotnet publish "Implementacija/Autosalon OneZone/Autosalon OneZone/Autosalon OneZone.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Autosalon OneZone.dll"]

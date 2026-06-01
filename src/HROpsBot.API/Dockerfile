FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/HROpsBot.API/HROpsBot.API.csproj", "src/HROpsBot.API/"]
COPY ["src/HROpsBot.Core/HROpsBot.Core.csproj", "src/HROpsBot.Core/"]
COPY ["src/HROpsBot.Domain/HROpsBot.Domain.csproj", "src/HROpsBot.Domain/"]
COPY ["src/HROpsBot.Infrastructure/HROpsBot.Infrastructure.csproj", "src/HROpsBot.Infrastructure/"]

RUN dotnet restore "src/HROpsBot.API/HROpsBot.API.csproj"

COPY . .
WORKDIR "/src/src/HROpsBot.API"
RUN dotnet build "HROpsBot.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HROpsBot.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HROpsBot.API.dll"]

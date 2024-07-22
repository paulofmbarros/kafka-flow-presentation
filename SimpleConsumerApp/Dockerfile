FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["SimpleComsumerApp/SimpleComsumerApp.csproj", "SimpleComsumerApp/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "SimpleComsumerApp/SimpleComsumerApp.csproj"
COPY . .
WORKDIR "/src/SimpleComsumerApp"
RUN dotnet build "SimpleComsumerApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "SimpleComsumerApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SimpleComsumerApp.dll"]

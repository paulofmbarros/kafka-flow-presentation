FROM mcr.microsoft.com/dotnet/core/runtime:3.1 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["ConsumerApp/ConsumerApp.csproj", "ConsumerApp/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "ConsumerApp/ConsumerApp.csproj"
COPY . .
WORKDIR "/src/ConsumerApp"
RUN dotnet build "ConsumerApp.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ConsumerApp.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsumerApp.dll"]

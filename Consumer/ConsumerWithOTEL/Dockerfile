FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Consumer/ConsumerWithOTEL/ConsumerWithOTEL.csproj", "Consumer/ConsumerWithOTEL/"]
COPY ["Common/Common.csproj", "Common/"]
RUN dotnet restore "Consumer/ConsumerWithOTEL/ConsumerWithOTEL.csproj"
COPY . .
WORKDIR "/src/Consumer/ConsumerWithOTEL"
RUN dotnet build "ConsumerWithOTEL.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ConsumerWithOTEL.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ConsumerWithOTEL.dll"]

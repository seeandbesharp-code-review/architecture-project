# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["ChineseAuction.EventConsumer/ChineseAuction.EventConsumer.csproj", "ChineseAuction.EventConsumer/"]
RUN dotnet restore "ChineseAuction.EventConsumer/ChineseAuction.EventConsumer.csproj"

COPY . .
WORKDIR /src/ChineseAuction.EventConsumer
RUN dotnet publish "ChineseAuction.EventConsumer.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "ChineseAuction.EventConsumer.dll"]

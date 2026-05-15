# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Chinese_Auction/Chinese_Auction.csproj", "Chinese_Auction/"]
RUN dotnet restore "Chinese_Auction/Chinese_Auction.csproj"

COPY . .
WORKDIR /src/Chinese_Auction
RUN dotnet publish "Chinese_Auction.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:80
EXPOSE 80
ENTRYPOINT ["dotnet", "Chinese_Auction.dll"]

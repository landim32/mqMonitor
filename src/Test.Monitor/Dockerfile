FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Test.Contracts/Test.Contracts.csproj", "Test.Contracts/"]
COPY ["src/Test.Infrastructure/Test.Infrastructure.csproj", "Test.Infrastructure/"]
COPY ["src/Test.Monitor/Test.Monitor.csproj", "Test.Monitor/"]
RUN dotnet restore "Test.Monitor/Test.Monitor.csproj"
COPY src/Test.Contracts/ Test.Contracts/
COPY src/Test.Infrastructure/ Test.Infrastructure/
COPY src/Test.Monitor/ Test.Monitor/
WORKDIR /src/Test.Monitor
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Test.Monitor.dll"]

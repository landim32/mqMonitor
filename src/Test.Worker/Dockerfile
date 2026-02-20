FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Test.Contracts/Test.Contracts.csproj", "Test.Contracts/"]
COPY ["src/Test.Infrastructure/Test.Infrastructure.csproj", "Test.Infrastructure/"]
COPY ["src/Test.Worker/Test.Worker.csproj", "Test.Worker/"]
RUN dotnet restore "Test.Worker/Test.Worker.csproj"
COPY src/Test.Contracts/ Test.Contracts/
COPY src/Test.Infrastructure/ Test.Infrastructure/
COPY src/Test.Worker/ Test.Worker/
WORKDIR /src/Test.Worker
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Test.Worker.dll"]

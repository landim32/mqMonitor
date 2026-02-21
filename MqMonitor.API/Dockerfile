FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MqMonitor.DTO/MqMonitor.DTO.csproj", "MqMonitor.DTO/"]
COPY ["MqMonitor.Domain/MqMonitor.Domain.csproj", "MqMonitor.Domain/"]
COPY ["MqMonitor.Infra.Interfaces/MqMonitor.Infra.Interfaces.csproj", "MqMonitor.Infra.Interfaces/"]
COPY ["MqMonitor.Infra/MqMonitor.Infra.csproj", "MqMonitor.Infra/"]
COPY ["MqMonitor.Application/MqMonitor.Application.csproj", "MqMonitor.Application/"]
COPY ["MqMonitor.API/MqMonitor.API.csproj", "MqMonitor.API/"]
RUN dotnet restore "MqMonitor.API/MqMonitor.API.csproj"
COPY MqMonitor.DTO/ MqMonitor.DTO/
COPY MqMonitor.Domain/ MqMonitor.Domain/
COPY MqMonitor.Infra.Interfaces/ MqMonitor.Infra.Interfaces/
COPY MqMonitor.Infra/ MqMonitor.Infra/
COPY MqMonitor.Application/ MqMonitor.Application/
COPY MqMonitor.API/ MqMonitor.API/
WORKDIR /src/MqMonitor.API
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MqMonitor.API.dll"]

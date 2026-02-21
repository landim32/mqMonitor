FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG PROJECT_NAME
WORKDIR /src
COPY ["MqMonitor.DTO/MqMonitor.DTO.csproj", "MqMonitor.DTO/"]
COPY ["MqMonitor.Domain/MqMonitor.Domain.csproj", "MqMonitor.Domain/"]
COPY ["MqMonitor.Infra.Interfaces/MqMonitor.Infra.Interfaces.csproj", "MqMonitor.Infra.Interfaces/"]
COPY ["MqMonitor.Infra/MqMonitor.Infra.csproj", "MqMonitor.Infra/"]
COPY ["MqMonitor.Application/MqMonitor.Application.csproj", "MqMonitor.Application/"]
COPY ["examples/${PROJECT_NAME}/${PROJECT_NAME}.csproj", "examples/${PROJECT_NAME}/"]
RUN dotnet restore "examples/${PROJECT_NAME}/${PROJECT_NAME}.csproj"
COPY MqMonitor.DTO/ MqMonitor.DTO/
COPY MqMonitor.Domain/ MqMonitor.Domain/
COPY MqMonitor.Infra.Interfaces/ MqMonitor.Infra.Interfaces/
COPY MqMonitor.Infra/ MqMonitor.Infra/
COPY MqMonitor.Application/ MqMonitor.Application/
COPY examples/${PROJECT_NAME}/ examples/${PROJECT_NAME}/
WORKDIR /src/examples/${PROJECT_NAME}
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
ARG PROJECT_NAME
ENV DLL_NAME=${PROJECT_NAME}.dll
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT dotnet $DLL_NAME

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Finance.BackgroundTasks/Finance.BackgroundTasks.csproj Finance.BackgroundTasks/
COPY Finance.Application/Finance.Application.csproj Finance.Application/
COPY Finance.Infrastructure/Finance.Infrastructure.csproj Finance.Infrastructure/
COPY Finance.Domain/Finance.Domain.csproj Finance.Domain/

RUN dotnet restore Finance.BackgroundTasks/Finance.BackgroundTasks.csproj

COPY Finance.BackgroundTasks/ Finance.BackgroundTasks/
COPY Finance.Application/ Finance.Application/
COPY Finance.Infrastructure/ Finance.Infrastructure/
COPY Finance.Domain/ Finance.Domain/

RUN dotnet publish Finance.BackgroundTasks/Finance.BackgroundTasks.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Finance.BackgroundTasks.dll"]
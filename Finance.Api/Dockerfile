FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY Finance.Api/Finance.Api.csproj Finance.Api/
COPY Finance.Application/Finance.Application.csproj Finance.Application/
COPY Finance.Infrastructure/Finance.Infrastructure.csproj Finance.Infrastructure/
COPY Finance.Domain/Finance.Domain.csproj Finance.Domain/

RUN dotnet restore Finance.Api/Finance.Api.csproj

COPY Finance.Api/ Finance.Api/
COPY Finance.Application/ Finance.Application/
COPY Finance.Infrastructure/ Finance.Infrastructure/
COPY Finance.Domain/ Finance.Domain/

RUN dotnet publish Finance.Api/Finance.Api.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 5055

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "Finance.Api.dll"]
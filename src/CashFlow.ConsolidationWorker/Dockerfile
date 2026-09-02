FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CashFlow.slnx .
COPY src/CashFlow.Domain/CashFlow.Domain.csproj src/CashFlow.Domain/
COPY src/CashFlow.Application/CashFlow.Application.csproj src/CashFlow.Application/
COPY src/CashFlow.Infrastructure/CashFlow.Infrastructure.csproj src/CashFlow.Infrastructure/
COPY src/CashFlow.Api/CashFlow.Api.csproj src/CashFlow.Api/
COPY src/CashFlow.ConsolidationWorker/CashFlow.ConsolidationWorker.csproj src/CashFlow.ConsolidationWorker/
COPY tests/CashFlow.Domain.Tests/CashFlow.Domain.Tests.csproj tests/CashFlow.Domain.Tests/
COPY tests/CashFlow.Application.Tests/CashFlow.Application.Tests.csproj tests/CashFlow.Application.Tests/
COPY tests/CashFlow.Api.Tests/CashFlow.Api.Tests.csproj tests/CashFlow.Api.Tests/
RUN dotnet restore src/CashFlow.ConsolidationWorker/CashFlow.ConsolidationWorker.csproj

COPY . .
RUN dotnet publish src/CashFlow.ConsolidationWorker/CashFlow.ConsolidationWorker.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "CashFlow.ConsolidationWorker.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Vidriera.slnx .
COPY src/Vidriera.Domain/Vidriera.Domain.csproj src/Vidriera.Domain/
COPY src/Vidriera.Application/Vidriera.Application.csproj src/Vidriera.Application/
COPY src/Vidriera.Infrastructure/Vidriera.Infrastructure.csproj src/Vidriera.Infrastructure/
COPY src/Vidriera.Api/Vidriera.Api.csproj src/Vidriera.Api/
RUN dotnet restore src/Vidriera.Api/Vidriera.Api.csproj

COPY src/ src/
COPY db/ db/
RUN dotnet publish src/Vidriera.Api/Vidriera.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app .

ENV ASPNETCORE_ENVIRONMENT=Production
ENTRYPOINT ["dotnet", "Vidriera.Api.dll"]

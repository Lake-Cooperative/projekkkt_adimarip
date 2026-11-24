FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
# Сначала только проекты — слой restore кэшируется, пока не меняются зависимости.
COPY backend/Adimarip.Domain/Adimarip.Domain.csproj backend/Adimarip.Domain/
COPY backend/Adimarip.Application/Adimarip.Application.csproj backend/Adimarip.Application/
COPY backend/Adimarip.Infrastructure/Adimarip.Infrastructure.csproj backend/Adimarip.Infrastructure/
COPY backend/Adimarip.Api/Adimarip.Api.csproj backend/Adimarip.Api/
RUN dotnet restore backend/Adimarip.Api/Adimarip.Api.csproj
COPY backend/Adimarip.Domain/ backend/Adimarip.Domain/
COPY backend/Adimarip.Application/ backend/Adimarip.Application/
COPY backend/Adimarip.Infrastructure/ backend/Adimarip.Infrastructure/
COPY backend/Adimarip.Api/ backend/Adimarip.Api/
RUN dotnet publish backend/Adimarip.Api/Adimarip.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081
ENTRYPOINT ["dotnet", "Adimarip.Api.dll"]

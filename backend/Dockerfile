FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY backend/Adimarip.Api/Adimarip.Api.csproj backend/Adimarip.Api/
COPY adimarip-efcore/Adimarip.Data/Adimarip.Data.csproj adimarip-efcore/Adimarip.Data/
RUN dotnet restore backend/Adimarip.Api/Adimarip.Api.csproj
COPY backend/Adimarip.Api/ backend/Adimarip.Api/
COPY adimarip-efcore/Adimarip.Data/ adimarip-efcore/Adimarip.Data/
RUN dotnet publish backend/Adimarip.Api/Adimarip.Api.csproj --configuration Release --no-restore --output /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./
ENV ASPNETCORE_URLS=http://+:8081
EXPOSE 8081
ENTRYPOINT ["dotnet", "Adimarip.Api.dll"]

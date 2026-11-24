# Сборка «одним контейнером» для облачных хостингов (netrun, Railway, Render и т.п.):
# интерфейс собирается в статику и раздаётся самим API. База данных — отдельный сервис хостинга.
# Локальная разработка по-прежнему идёт через docker_compose.yml (три контейнера).

FROM node:20-alpine AS frontend
WORKDIR /src/frontend
COPY frontend/package.json frontend/package-lock.json ./
RUN npm ci --no-audit --no-fund
COPY frontend/ ./
# Пустой адрес API = тот же домен, что и интерфейс.
ENV VITE_API_URL=
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS backend
WORKDIR /src
COPY backend/Adimarip.Domain/Adimarip.Domain.csproj backend/Adimarip.Domain/
COPY backend/Adimarip.Application/Adimarip.Application.csproj backend/Adimarip.Application/
COPY backend/Adimarip.Infrastructure/Adimarip.Infrastructure.csproj backend/Adimarip.Infrastructure/
COPY backend/Adimarip.Api/Adimarip.Api.csproj backend/Adimarip.Api/
RUN dotnet restore backend/Adimarip.Api/Adimarip.Api.csproj
COPY backend/ backend/
RUN dotnet publish backend/Adimarip.Api/Adimarip.Api.csproj --configuration Release --no-restore --output /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=backend /app ./
COPY --from=frontend /src/frontend/dist ./wwwroot
# FORWARDEDHEADERS: хостинг стоит прокси перед приложением — берём реальный IP клиента из X-Forwarded-For
# (иначе лимит попыток входа станет общим на всех пользователей).
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=1
# Порт задаёт хостинг через PORT (по умолчанию 8080).
ENV PORT=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Adimarip.Api.dll"]

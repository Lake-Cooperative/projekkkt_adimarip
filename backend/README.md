# ASP.NET Core API — семинар 2

Из корня репозитория:

```bash
dotnet run --project backend/Adimarip.Api/Adimarip.Api.csproj --launch-profile Adimarip.Api
curl -i http://localhost:8081/api/health
```

Требуется .NET SDK 8 и доступ к NuGet при первом восстановлении пакетов. Ожидаемый ответ: HTTP 200, `{"status":"ok","service":"adimarip-api"}`. Swagger включён в режиме Development: `http://localhost:8081/swagger`.

Библиотека `adimarip-efcore/Adimarip.Data` уже присутствовала во входном проекте. `AdimaripDbContext` регистрируется только при заданной `ConnectionStrings__Default`, но **миграции автоматически не применяются**, ни одной ручки CRUD и JWT пока нет. Перед применением миграций в следующем семинаре требуется нормализовать исходный EF snapshot/metadata и протестировать схему.

Разрешённые CORS-источники по умолчанию: `http://localhost:8080` и `http://localhost:5173`; в Compose переопределяются переменными окружения.

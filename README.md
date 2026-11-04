# Camino Seguro — Backend (.NET 8 + SQL Server)

API REST que cumple el contrato de `src/app/core/models.ts` del frontend. Base: `/api`, JSON en camelCase.

## Correr en local (Windows)

1. Requisitos: **.NET 8 SDK** (`dotnet --version` → 8.x) y **SQL Server** (Express, Developer o LocalDB).
2. Revisa la conexión en `CaminoSeguro.Api/appsettings.Development.json`:
   - SQL Express: `Server=localhost\\SQLEXPRESS;...` (viene así)
   - SQL Developer / instancia por defecto: `Server=localhost;...`
   - LocalDB: `Server=(localdb)\\MSSQLLocalDB;...`
3. Ejecuta:
   ```cmd
   cd camino_seguroback\CaminoSeguro.Api
   dotnet dev-certs https --trust
   dotnet run --launch-profile https
   ```
   Al arrancar **crea la base `CaminoSeguro`, las tablas y la semilla** (18 municipios y 6 categorías). No hace falta `dotnet ef`.
4. Swagger: https://localhost:7001/swagger. En desarrollo existe la usuaria `demo@demo.com` / `Demo1234`.
5. En el front (`src/environments/environment.development.ts`): `apiUrl: 'https://localhost:7001/api'` y `usarMock: false`.

## Endpoints

| Método | Ruta | Token | Respuesta |
|---|---|---|---|
| POST | /api/auth/registro | No | 201 `AuthResponse` · 409 correo repetido |
| POST | /api/auth/login | No | 200 `AuthResponse` · 401 |
| GET | /api/municipios | No | `Municipio[]` |
| GET | /api/categorias | No | `Categoria[]` |
| GET | /api/reportes?lat&lng · ?municipioRefId · &categoriaId | Opcional | `Reporte[]` por `distanciaKm` asc |
| GET | /api/reportes/{id} | Opcional | `Reporte` · 404 |
| GET | /api/reportes/mios | Sí | `Reporte[]` |
| POST | /api/reportes | Sí | 201 `Reporte` |
| PUT | /api/reportes/{id} | Sí (autora) | 200 `Reporte` · 403 |
| DELETE | /api/reportes/{id} | Sí (autora) | 204 · 403 |
| POST | /api/reportes/{id}/confirmaciones | Sí | 204 · 400 si es propio |
| DELETE | /api/reportes/{id}/confirmaciones | Sí | 204 |
| GET | /api/salud | No | estado de la API |

Errores: `{ "message": "..." }` o ProblemDetails de validación (`errors`).

## Decisiones de seguridad (para la sustentación)
- Contraseñas con `PasswordHasher` de ASP.NET Core Identity (PBKDF2 + sal). Login con tiempo constante para no revelar si el correo existe.
- JWT HS256 de 2 h; el token solo lleva el id y el alias, **nunca el correo**. `autor` es el alias o "Anónima".
- Autorización real en el backend: solo la autora edita o borra (403); no se puede confirmar un reporte propio.
- Rate limiting: 10/min por IP en login y registro; 20/min por usuaria al escribir.
- CORS con lista blanca de orígenes (sin `*`). Validación de entrada con DataAnnotations; EF Core usa consultas parametrizadas (anti-SQL injection).
- Nominatim con User-Agent propio y timeout de 5 s; si falla o el resultado queda a más de 25 km del municipio, se usa el centro del municipio.

## Producción / Docker
Variables de entorno: `ConnectionStrings__Default`, `Jwt__Key` (mínimo 32 caracteres), `Cors__AllowedOrigins` (separados por comas), `Swagger__Habilitado=true` si se quiere Swagger en la nube.
```bash
docker build -t camino-seguro-api .
docker run -p 8080:8080 -e ConnectionStrings__Default="..." -e Jwt__Key="..." -e Cors__AllowedOrigins="https://tu-app.netlify.app" camino-seguro-api
```

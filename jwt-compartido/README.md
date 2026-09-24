# JWT Compartido - Demo de 3 Minimal APIs

Demuestra que un JWT firmado con una clave secreta compartida (HMAC SHA-256)
sirve para autenticarse en varias APIs distintas. Solo una de ellas tiene login;
el token que emite es aceptado por las otras dos porque las tres validan con la
misma clave, el mismo issuer y la misma audience.

## Las 3 APIs

| API         | Puerto | Login            | Endpoint protegido | Swagger                        |
|-------------|--------|------------------|--------------------|--------------------------------|
| AuthApi     | 5001   | si (`POST /login`) | `GET /me`          | http://localhost:5001/swagger  |
| OrdersApi   | 5002   | no               | `GET /orders`      | http://localhost:5002/swagger  |
| CatalogApi  | 5003   | no               | `GET /catalog`     | http://localhost:5003/swagger  |

Solo `AuthApi` emite tokens. `OrdersApi` y `CatalogApi` unicamente los validan.

## Por que funciona el token cross-API

Las tres leen la misma configuracion JWT. En `docker-compose.yml` se inyecta por
variables de entorno (sobreescriben `appsettings.json`):

```
Jwt__Key=clave-super-secreta-compartida-entre-las-3-apis-cambiar-en-prod
Jwt__Issuer=jwt-compartido-demo
Jwt__Audience=jwt-compartido-demo
```

Como la clave de firma es la misma, la firma del token emitido por AuthApi se
valida correctamente en las otras dos. Si cambias la clave en una sola de ellas,
esa API empezara a rechazar el token con 401.

## Levantar todo (modo dev, con Swagger)

Desde esta carpeta:

```bash
docker compose up --build
```

Quedan disponibles los tres Swagger:

- http://localhost:5001/swagger  (AuthApi)
- http://localhost:5002/swagger  (OrdersApi)
- http://localhost:5003/swagger  (CatalogApi)

Para apagar: `docker compose down`.

## Probar desde Swagger

1. Abre http://localhost:5001/swagger.
2. Ejecuta `POST /login` con:
   ```json
   { "username": "admin", "password": "admin123" }
   ```
   Copia el valor de `access_token`.
3. Abre el Swagger de cualquier API (5001, 5002 o 5003), pulsa **Authorize**,
   pega solo el token (sin la palabra `Bearer`) y confirma.
4. Ejecuta el endpoint protegido (`/me`, `/orders` o `/catalog`). Responde 200
   con el mismo token en las tres.

Usuarios demo validos: `admin` / `admin123` y `rogelio` / `demo`.

## Probar desde la terminal (curl)

```bash
# 1. Obtener token desde AuthApi
TOKEN=$(curl -s -X POST http://localhost:5001/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}' | jq -r .access_token)

# 2. El MISMO token funciona en las 3 APIs
curl -s http://localhost:5001/me      -H "Authorization: Bearer $TOKEN"
curl -s http://localhost:5002/orders  -H "Authorization: Bearer $TOKEN"
curl -s http://localhost:5003/catalog -H "Authorization: Bearer $TOKEN"

# 3. Sin token -> 401
curl -s -o /dev/null -w "%{http_code}\n" http://localhost:5002/orders
```

## Correr una sola API sin Docker

```bash
cd AuthApi      # o OrdersApi / CatalogApi
dotnet run
```

Lee la configuracion de `appsettings.json` (ya trae la misma clave en las tres).

## Stack

- .NET 10 / ASP.NET Core 10 Minimal APIs
- `Microsoft.AspNetCore.Authentication.JwtBearer` para validar el token
- `Swashbuckle.AspNetCore` para Swagger UI con boton Authorize
- Docker multi-stage (sdk:10.0 para build, aspnet:10.0 para runtime)

## Nota de seguridad

La clave esta en texto plano solo por ser una demo local. En un entorno real va
en un gestor de secretos (variables de entorno seguras, Key Vault, etc.) y nunca
se commitea.

# Control de acceso y bitacora de acciones (SaaS multi-tenant)

Ejemplo ejecutable de dos patrones del catalogo que casi nunca se implementan por separado:
**quien puede hacer algo** y **el registro de que lo hizo**.

La gracia no esta en ninguno de los dos por su cuenta, sino en el cruce: **toda accion sensible
pasa por el permiso Y deja rastro, incluidos los intentos que se rechazaron.** Un sistema que
solo registra lo que si ocurrio no puede responder la pregunta que de verdad se hace despues de
un incidente, que es "quien intento".

| | |
|---|---|
| **Skills** | `access-control`, `action-audit-log` (principales) · `master-data-crud`, `multi-tenancy`, `tenant-entity-chassis`, `ef-core-data-access`, `dapper-data-access` |
| **Tenencia** | multi-tenant |
| **Motor** | PostgreSQL 16 |
| **Datos** | EF Core escribe, Dapper lee |
| **Frontend** | React 19 + Vite + Tailwind, i18n es/en |

---

## Que demuestra

**Del control de acceso**

- El **catalogo de permisos vive en codigo** y un seeder lo sincroniza en cada arranque. Por eso
  la matriz de roles no tiene un boton de "agregar permiso": uno que el codigo no declara no
  existe, y ofrecer darlo de alta desde la pantalla seria ofrecer crear permisos fantasma.
- Politica por endpoint y concesion por rol, con el camino completo a la vista:
  **pertenencia -> rol -> permiso**. Es lo que convierte un 403 en un diagnostico.
- **Bandera de enforcement** para migrar sin bloquear a nadie. Apagada, el sistema **registra lo
  que habria bloqueado** en vez de bloquearlo. Esas filas son la lista de a quien vas a romper el
  dia que la enciendas.
- Ocultamiento en el frontend: lo que no esta en el mapa ruta-permiso se dibuja siempre; lo que
  esta, solo si la persona puede. Y es **cortesia, no seguridad**: cada endpoint valida por su
  cuenta y responde 403 aunque el boton nunca se haya visto.

**De la bitacora**

- Append-only. La pantalla **no tiene acciones**: ni editar, ni borrar, ni marcar como visto. Una
  bitacora que se puede tocar desde la interfaz deja de servir para lo unico que sirve.
- El **efecto que no comparte transaccion** con la accion tiene su propia columna. La accion se
  guardo en nuestra base; el efecto de afuera pudo no aplicarse, y sin ese dato "quedo
  registrado" se confunde con "quedo hecho".
- Motivo obligatorio en las acciones destructivas, y el motivo se lee completo.
- Filtros, orden y paginado **en servidor**. Con 150 mil renglones no es una preferencia: filtrar
  en el cliente solo podria mirar los 25 de la pagina y devolveria una lista vacia sin decir por
  que.

---

## Como levantarlo

```bash
# 1. Motor. Nada que instalar a mano. Escucha en 55440 para no chocar con otro PostgreSQL.
docker compose up -d

# 2. Esquema y catalogo de permisos. Tambien corren solos al arrancar el Host.
cd backend/SaasAccessAudit.Host && dotnet run -- migrate

# 3. Datos: 2 tenants, 40 usuarios cada uno, 150 mil renglones de bitacora sobre 180 dias.
dotnet run -- seed

# 4. API en http://localhost:5090
dotnet run

# 5. Frontend en http://localhost:5190 (proxy /api -> :5090)
cd ../../frontend && npm install && npm run dev
```

El selector de arriba a la derecha cambia de **empresa** y de **persona**. Es andamiaje del
ejemplo -en un sistema real eso lo decide el token- y esta ahi porque es la forma mas rapida de
ver las dos cosas que importan: que la bitacora de un tenant no muestra ni un renglon del otro, y
que la misma pantalla cambia segun los permisos de quien mira.

---

## Que mirar primero

Cuatro archivos. El resto es andamiaje.

| Archivo | Por que |
|---|---|
| `Modules/Access/Access.Contracts/PermissionCatalog.cs` | El catalogo en codigo. La fuente de verdad de que permisos existen |
| `Modules/Audit/Audit.Infrastructure/Persistence/Sql/MainDb/AuditSql.cs` | El SQL a mano de la bitacora, y donde se compone el aislamiento de tenant |
| `Modules/Audit/Audit.Tests/TenantIsolationSqlTests.cs` | **La red de seguridad.** Falla si una consulta de negocio aparece sin aislar |
| `frontend/src/features/shell/AppShell.jsx` | El mapa ruta-permiso, que es lo unico que decide que aparece en el menu |

---

## Lo mas delicado: Dapper leyendo en un proyecto con tenancy

La regla de oro 2 del catalogo dice que la tenencia decide el acceso a datos: con tenant, manda
EF Core, porque el aislamiento se aplica solo por global query filter. Y lista como **antipatron**
usar Dapper con tenancy y filtrar el tenant a mano en cada consulta.

Aqui hay Dapper igualmente, y conviene entender por que y a que precio.

**Por que.** Las consultas de la bitacora son paginado real sobre 150 mil renglones con siete
filtros opcionales y `COUNT(1) OVER()`. Eso es SQL a mano; expresarlo con EF produce consultas
que nadie puede leer ni predecir. **EF escribe y Dapper lee**, y cada uno hace lo que hace bien.

**El precio.** Dapper **no ve** el query filter de EF. Una consulta cruda sin `tenant_id` devuelve
la bitacora de todos los clientes, y no falla: devuelve datos de mas, con buena cara. Es
exactamente el antipatron que la regla advierte.

**Como se paga.** Tres cosas, y las tres tienen que estar:

1. El filtro de tenant vive en **un solo fragmento** que toda consulta compone. Nadie escribe
   `WHERE tenant_id = @TenantId` a mano.
2. El `tenant_id` **nunca** llega del cliente: sale del contexto de la peticion, igual que en EF.
3. **Una prueba recorre las consultas por nivel y falla si una tabla de negocio aparece sin
   aislar.** Sin esa red, esto es una fuga esperando su turno; con ella, el descuido se detecta
   en el build y no en una auditoria.

Si copias este ejemplo y te llevas Dapper pero no la prueba, te llevaste el riesgo sin la
defensa.

---

## Mapa del repositorio

```
backend/
  Common/                      mediador, envelope de respuesta, resultados
  Shared/                      acceso a datos, contexto de tenant, chasis de entidad
  Modules/
    Access/                    permisos, roles, usuarios, enforcement
    Audit/                     bitacora de acciones e intentos rechazados
      *.Domain                 entidades y contratos de repositorio
      *.Application            casos de uso: Request / Handler / Responses
      *.Infrastructure         EF (escritura) y Dapper (lectura)
      *.Presentation           controllers y presenters
      *.Tests
  SaasAccessAudit.Host/        cableado, migraciones, seeders
frontend/
  src/api/                     cliente HTTP y servicios por modulo
  src/features/                users, roles, audit, shell
  src/i18n/                    es / en
  src/styles/designSystem.js   tokens; cero clases sueltas en las pantallas
```

---

## Estado

```
dotnet build     0 errores, 0 warnings
dotnet test      750 pruebas en verde  (Access 62, Audit 688)
npm run build    verde
```

---

## Lo que este ejemplo NO hace

- **No es un sistema de autenticacion.** El selector de identidad y el emisor de tokens son
  andamiaje para poder cambiar de persona en dos clics. En un sistema real eso es tu proveedor de
  identidad, y no se copia de aqui.
- **No tiene modo oscuro ni tercer idioma.** Quedan fuera de alcance a proposito: los cubre
  `theming-dark-mode` y el ejemplo de notificaciones.
- **No purga la bitacora.** Una tabla append-only crece para siempre, y una politica de retencion
  es una decision de negocio y de cumplimiento, no un detalle tecnico que un ejemplo pueda
  suponer por ti.
- **El `synchronous_commit=off` del `docker-compose.yml` es de desarrollo.** Cambia durabilidad
  por velocidad para que sembrar 150 mil renglones tarde segundos. En produccion no se hace.

# Servicios con Docker Compose

Coleccion de `docker compose` listos para levantar servicios de apoyo en local:
bases de datos, correo de prueba, observabilidad, CI, identidad y gestion de Docker.
Cada carpeta es independiente: entra y corre `docker compose up -d`.

> Son para **desarrollo y laboratorio**, no para produccion. Varios traen las
> credenciales por defecto de la imagen (`admin` / `admin`) a proposito, para que
> arranquen sin configurar nada. Si expones alguno fuera de tu maquina, cambialas.

## Indice

| Categoria | Servicio | Puertos | Para que |
|---|---|---|---|
| Bases de datos | [sqlserver-2022](bases-de-datos/sqlserver-2022) | 1433 | SQL Server 2022 (requiere `.env`) |
| | [redis](bases-de-datos/redis) | 6379 | Cache / colas |
| Correo | [mailpit](correo/mailpit) | 8025 web, 1025 SMTP | Atrapa correos de prueba; trae ejemplo en Node |
| | [mailhog](correo/mailhog) | 8025 web, 1025 SMTP | Alternativa mas vieja a Mailpit |
| | [mailcow](correo/mailcow) | 25, 587, 993, 143, 80, 443 | Servidor de correo completo (esqueleto) |
| Observabilidad | [grafana-prometheus](observabilidad/grafana-prometheus) | 3000, 9090 | Metricas y tableros |
| | [seq](observabilidad/seq) | 5341 | Logs estructurados |
| CI y calidad | [jenkins](ci-y-calidad/jenkins) | 8080, 50000 | CI con usuario admin precreado |
| | [sonarqube](ci-y-calidad/sonarqube) | 9000 | Analisis estatico |
| | [stackhawk](ci-y-calidad/stackhawk) | - | Guia de escaneo DAST de una API |
| Identidad y secretos | [keycloak](identidad-y-secretos/keycloak) | 8080 | Identity provider (OIDC) |
| | [vault](identidad-y-secretos/vault) | 8200 | Gestion de secretos |
| APIs | [mockserver](apis/mockserver) | 1080 | Simular APIs externas |
| | [redoc](apis/redoc) | 3002 | Publicar un OpenAPI (`spec/openapi.yaml`) |
| Gestion de Docker | [portainer](gestion-docker/portainer) | 9000, 9443 | UI para administrar Docker |
| | [dockge](gestion-docker/dockge) | 5001 | UI para administrar stacks de Compose |
| | [watchtower](gestion-docker/watchtower) | - | Actualiza contenedores solos (requiere `.env`) |
| | [scripts/docker-cleanup.sh](gestion-docker/scripts/docker-cleanup.sh) | - | Libera espacio (**borra** imagenes y volumenes sin usar) |
| Apps | [nextcloud](apps/nextcloud) | 8083 | Almacenamiento de archivos |

**Puertos que chocan:** mailpit/mailhog (8025, 1025), jenkins/keycloak (8080) y
sonarqube/portainer (9000). No levantes esas parejas a la vez sin cambiar uno.

## Convenciones

- **Secretos en `.env`, nunca en el YAML.** Donde hace falta uno hay un
  `.env.example`: copialo como `.env` y rellenalo. El `.env` esta ignorado por git.
- **Datos en carpetas `*-data/`** junto al compose (ignoradas por git). Redis,
  Mailpit y Mailhog aceptan `DOCKER_VOLUME_ABSOLUTE_PATH` para guardarlos en otra
  ruta; sin definirla, usan la carpeta actual.
- Lo que generan Portainer, Dockge y SonarQube al correr (bases, llaves privadas,
  certificados) tambien esta ignorado: es estado de tu maquina, no configuracion.

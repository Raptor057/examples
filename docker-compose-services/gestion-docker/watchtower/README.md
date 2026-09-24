# 🛡️ Watchtower - Actualización automática de contenedores Docker

## 📍 ¿Qué es Watchtower?

[Watchtower](https://containrrr.dev/watchtower/) es una herramienta ligera que se ejecuta como un contenedor dentro de Docker y se encarga de **supervisar tus contenedores activos**. Su objetivo principal es:

- Detectar si existe una nueva versión de la imagen que estás usando.
- Descargar la nueva versión.
- Detener el contenedor actual.
- Volver a lanzarlo automáticamente con la imagen actualizada.
  
Esto te permite **mantener tus servicios Docker actualizados** sin tener que hacerlo manualmente. Es especialmente útil para entornos de producción donde quieres que los contenedores estén siempre al día, sin intervención humana.

---

## 🚀 ¿Qué hace este `docker-compose.yml`?

Este archivo levanta un contenedor Watchtower que:

- Se ejecuta en segundo plano y se reinicia automáticamente si se detiene.
- Consulta cada 5 minutos (300 segundos) si hay actualizaciones.
- Limpia imágenes viejas para liberar espacio.
- Monitorea incluso contenedores detenidos.
- Envía correos de notificación al detectar o realizar una actualización.

---

## 🧱 Contenido del `docker-compose.yml`

```yaml
services:
  watchtower:
    container_name: watchtower
    image: containrrr/watchtower:latest
    restart: unless-stopped
    volumes:
      - /var/run/docker.sock:/var/run/docker.sock
    environment:
      - WATCHTOWER_POLL_INTERVAL=300             # Verifica actualizaciones cada 5 minutos
      - WATCHTOWER_CLEANUP=true                  # Elimina imágenes obsoletas
      - WATCHTOWER_INCLUDE_STOPPED=true          # Revisa contenedores detenidos también
      - WATCHTOWER_NOTIFICATIONS=email           # Habilita notificaciones por correo
      - WATCHTOWER_NOTIFICATION_EMAIL_FROM=correo@ejemplo.com
      - WATCHTOWER_NOTIFICATION_EMAIL_TO=correo1@example.com,correo2@example.com
      - WATCHTOWER_NOTIFICATION_EMAIL_SERVER=smtp.gmail.com
      - WATCHTOWER_NOTIFICATION_EMAIL_SERVER_PORT=587
      - WATCHTOWER_NOTIFICATION_EMAIL_SERVER_USER=servicio@ejemplo.com
      - WATCHTOWER_NOTIFICATION_EMAIL_SERVER_PASSWORD=************
      - WATCHTOWER_NOTIFICATION_EMAIL_DELAY=2
      - WATCHTOWER_NOTIFICATIONS_LEVEL=info

```

> ⚠️ **IMPORTANTE:** Usa variables de entorno o un archivo `.env` para evitar exponer credenciales sensibles directamente.

----------

## 📬 Notificaciones por Correo

Puedes recibir notificaciones al correo cada vez que Watchtower detecte una actualización, intente reiniciar un contenedor, o se produzca un error. Esto es útil para mantener trazabilidad y monitoreo continuo.

----------

## ✅ ¿Cómo iniciar Watchtower?

1.  Guarda el `docker-compose.yml`.
    
2.  Lanza el contenedor con:
    

```bash
docker compose up -d

```

3.  Listo. Watchtower empezará a monitorear todos tus contenedores.
    

----------

## 🧠 Buenas prácticas

-   Usa Watchtower con contenedores que no requieran pasos manuales post-deploy (como migraciones de DB).
    
-   Combínalo con herramientas de monitoreo como **Portainer**, **Prometheus** o **Grafana**.
    
-   Usa versiones `lts` o `taggeadas` de imágenes si no quieres actualizaciones no planeadas.
    

----------

## 📚 Enlaces útiles

-   [Documentación oficial de Watchtower](https://containrrr.dev/watchtower/)
    
-   [Código fuente en GitHub](https://github.com/containrrr/watchtower)
    
-   [Ejemplo de uso con Portainer](https://portainer.io/)
    

----------

🔒 **Nota de seguridad:** Evita usar contraseñas directamente en el YAML. Usa variables de entorno o servicios de secretos como Docker Secrets, HashiCorp Vault o AWS Secrets Manager si es posible.

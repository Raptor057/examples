# SQL Server 2022

```bash
cp .env.example .env      # y pon una SA_PASSWORD fuerte
docker compose up -d
```

Queda en `localhost:1433`, usuario `sa`. Los datos viven en el volumen con nombre
`sql-data`, asi que sobreviven a `docker compose down` (no a `down -v`).

Si falta `SA_PASSWORD` en el `.env`, Compose se niega a arrancar en vez de crear un
servidor con contrasena vacia.

## Politicas de reinicio (`restart:`)

| Modo | Comportamiento |
|---|---|
| `no` (default) | No se reinicia |
| `always` | Siempre se reinicia, incluso si lo detuviste manualmente |
| `unless-stopped` | Se reinicia a menos que tu lo hayas detenido con `docker stop` |
| `on-failure` | Solo se reinicia si termina con error (exit code distinto de 0) |

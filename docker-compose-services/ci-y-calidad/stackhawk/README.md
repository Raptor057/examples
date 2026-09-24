StackHawk es una herramienta de **seguridad para aplicaciones web y APIs**, diseñada para ser integrada dentro de tus pipelines de CI/CD. Su principal componente es **HawkScan**, un escáner que automatiza pruebas de seguridad (DAST – *Dynamic Application Security Testing*) en aplicaciones corriendo **localmente o remotamente**, sin requerir acceso al código fuente.

---

## 🛡️ ¿Qué hace StackHawk?

**En resumen:**  
Escanea tus endpoints HTTP/HTTPS (REST, GraphQL, gRPC, SOAP, etc.) y detecta vulnerabilidades comunes como:

- Inyección SQL
- XSS (Cross-Site Scripting)
- CSRF
- Problemas de autenticación
- Fallas en políticas de CORS
- Configuraciones inseguras

Y todo esto lo hace **automatizable**, **reproducible**, y **configurable vía YAML**.

---

## 🔧 ¿Cómo se usa?

1. **Configuras el `stackhawk.yml`** con datos de tu app (URL, tipo de autenticación, tipo de API, etc.)
2. **Corres el escáner en Docker** (local o en CI)
3. **Revisas los resultados** en la consola o en la plataforma web de StackHawk

### Ejemplo básico:
```yaml
app:
  applicationId: abc123-def456
  env: development
  host: http://localhost:3000
```

Luego lo ejecutas así:
```bash
docker run --rm -e API_KEY="tu_api_key" \
  -v $(pwd):/hawk:rw \
  stackhawk/hawkscan:latest
```

---

## 🧠 ¿Por qué usarlo?

- ✅ Se integra fácil con GitHub Actions, GitLab, Jenkins, Azure Pipelines, etc.
- ✅ Permite escaneo autenticado (cookies, JWT, OAuth, etc.)
- ✅ Compatible con OpenAPI, GraphQL, SOAP y gRPC
- ✅ Provee dashboards con resultados de seguridad
- ✅ Puedes customizar qué escanear y qué no con regex o paths

---

## 🧪 Para pruebas de seguridad continuas

StackHawk es ideal si:
- Tienes APIs/documentación Swagger/OpenAPI
- Necesitas integrar pruebas de seguridad a tu pipeline DevOps
- Quieres reducir vulnerabilidades en etapas tempranas
- Trabajas con GraphQL y necesitas validaciones profundas

---

¡Claro, va con todo 🔐! Vamos paso por paso para que puedas correr **StackHawk (HawkScan)** en tu proyecto.

---

### ✅ 1. **Crear cuenta y app en StackHawk**

1. Regístrate en [https://app.stackhawk.com](https://app.stackhawk.com)
2. Crea una aplicación y copia tu `Application ID`
3. Ve a tu perfil y copia tu `API Key` personal (la vas a usar para correr el contenedor)

---

### ✅ 2. **Crea tu archivo `stackhawk.yml`**

Este archivo debe estar en la raíz de tu repo/proyecto. Aquí va un ejemplo básico para una API REST local en el puerto 5000:

```yaml
app:
  applicationId: "AQUI_TU_APPLICATION_ID"
  env: "local"
  host: "http://localhost:5000"

hawk:
  scan:
    maxDurationMinutes: 10

tags:
  - name: project
    value: backend-api
```

Si usas OpenAPI (Swagger), agrégalo así:

```yaml
app:
  ...
  openApiConf:
    path: "/swagger/v1/swagger.json"
```

---

### ✅ 3. **Correr el escáner con Docker**

Desde tu terminal:

```bash
docker run --rm \
  -e API_KEY="tu_api_key_aqui" \
  -v $(pwd):/hawk:rw \
  stackhawk/hawkscan:latest
```

> Si estás en **PowerShell o CMD** en Windows:
```powershell
docker run --rm `
  -e API_KEY="tu_api_key_aqui" `
  -v ${PWD}:/hawk `
  stackhawk/hawkscan:latest
```

---

### ⚙️ 4. (Opcional) Agregar autenticación

Si tu API requiere login, puedes agregar autenticación `usernamePassword` o `token` en el YAML. ¿Tu API necesita login? ¿JWT, sesión, cookie? Te puedo ayudar a configurarlo también.

---

### ⚡ 5. (Opcional) Integrarlo en GitHub Actions

---

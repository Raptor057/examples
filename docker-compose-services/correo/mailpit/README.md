
# 📬 Enviar correos HTML con Node.js usando Mailpit como servidor SMTP

Este proyecto te permite enviar correos electrónicos (HTML o texto plano) desde un script en Node.js y verlos en **Mailpit**, un servidor SMTP local para pruebas.

---

## 🧰 Requisitos

- Tener [Node.js](https://nodejs.org/) instalado (v16 o superior)
- Tener [Docker](https://www.docker.com/) y [Docker Compose](https://docs.docker.com/compose/) instalados

---

## 🚀 Paso a paso para crear este proyecto

### 1. Crea el directorio del proyecto

```bash
mkdir mailpit-node
cd mailpit-node

```

### 2. Inicializa un proyecto Node.js

```bash
npm init -y

```

### 3. Instala la dependencia `nodemailer`

```bash
npm install nodemailer

```

### 4. Crea el archivo `send-mail.js`

```js
// send-mail.js
const nodemailer = require("nodemailer");

async function enviarCorreo() {
  const transporter = nodemailer.createTransport({
    host: "localhost",
    port: 1025,
    secure: false, // Mailpit no usa TLS por defecto
  });

  const info = await transporter.sendMail({
    from: '"Tú 👨‍💻" <tu@ejemplo.com>',
    to: "usuario@local.test",
    subject: "Correo de prueba con HTML",
    html: `
      <h1>¡Hola!</h1>
      <p>Este es un <strong>correo HTML</strong> enviado desde Node.js usando <code>nodemailer</code>.</p>
    `,
    text: "¡Hola! Este es un correo de prueba (versión texto).",
  });

  console.log("✅ Correo enviado: %s", info.messageId);
}

enviarCorreo().catch(console.error);

```

----------

## 🐳 Ejecuta Mailpit con Docker

Crea un archivo `docker-compose.yml` con el siguiente contenido:

```yaml
version: '3.8'

services:
  mailpit:
    image: axllent/mailpit
    container_name: mailpit
    ports:
      - "8025:8025"  # UI web
      - "1025:1025"  # SMTP

```

Luego levanta el contenedor:

```bash
docker compose up -d

```

----------

## ✉️ Enviar el correo

Con Mailpit ya corriendo, ejecuta tu script:

```bash
node send-mail.js

```

----------

## 📬 Ver tu correo en Mailpit

Abre tu navegador en:

[http://localhost:8025](http://localhost:8025/)

Ahí verás los correos enviados capturados por Mailpit. Puedes ver:

-   HTML
    
-   Texto plano
    
-   Headers
    
-   Archivos adjuntos (si agregas alguno)
    

----------

## ✅ Resultado esperado en consola

```bash
✅ Correo enviado: <random-id@localhost>

```

----------

## 🧼 Finalizar

Cuando termines, puedes apagar Mailpit con:

```bash
docker compose down

```

----------


### 📌 Lo apuntas como servidor SMTP en tus apps con:
```
SMTP_HOST=localhost #O IP
SMTP_PORT=1025
```

## 🧠 Recursos

-   [Mailpit GitHub](https://github.com/axllent/mailpit)
    
-   [Nodemailer Docs](https://nodemailer.com/about/)
    


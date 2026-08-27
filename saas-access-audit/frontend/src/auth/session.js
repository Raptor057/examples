import { API_BASE_URL } from '../config/env'

// El access token vive en una variable de MODULO, fuera del arbol de React y fuera de
// localStorage: lo que guarda localStorage lo lee cualquier XSS. Se pierde al recargar, y eso
// esta bien: se vuelve a pedir.
let accessToken = null
let currentIdentity = null

export function getAccessToken() {
  return accessToken
}

export function getCurrentIdentity() {
  return currentIdentity
}

export function clearSession() {
  accessToken = null
  currentIdentity = null
}

/**
 * Directorio de demostracion: empresas y personas para poder entrar como una u otra.
 * Es andamiaje del ejemplo. En un producto real aqui hay una pantalla de acceso.
 */
export async function fetchDirectory() {
  const response = await fetch(`${API_BASE_URL}/session/tenants`)
  const body = await response.json()
  if (!response.ok || body?.isSuccess === false) {
    throw new Error(body?.message ?? 'No fue posible leer el directorio.')
  }
  return body.data
}

/**
 * Pide un token para una persona de una empresa.
 *
 * Lo que el usuario elige aqui es con QUE CREDENCIALES entra, no que datos lee. El tenant y las
 * pertenencias viajan FIRMADOS en el token: cambiar el parametro no cambia lo que la API deja
 * ver, y por eso la pantalla puede ofrecer el selector sin abrir un hueco.
 */
export async function signIn(tenantCode, username) {
  const response = await fetch(`${API_BASE_URL}/session/token`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ tenantCode, username }),
  })

  const body = await response.json()
  if (!response.ok || body?.isSuccess === false) {
    clearSession()
    const error = new Error(body?.message ?? 'No fue posible iniciar sesion.')
    error.envelope = body
    throw error
  }

  accessToken = body.data.accessToken
  currentIdentity = {
    tenantCode: body.data.tenantCode,
    tenantName: body.data.tenantName,
    username: body.data.username,
    displayName: body.data.displayName,
    groups: body.data.groups ?? [],
  }

  return currentIdentity
}

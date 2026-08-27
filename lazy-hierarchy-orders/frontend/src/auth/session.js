import { API_BASE_URL } from '../config/env'

// El access token vive en una variable de MODULO, fuera del arbol de React y fuera de
// localStorage: lo que guarda localStorage lo lee cualquier XSS. Se pierde al recargar, y eso
// esta bien: se vuelve a pedir.
let accessToken = null
let currentTenant = null
let pendingRequest = null

export function getAccessToken() {
  return accessToken
}

export function getCurrentTenant() {
  return currentTenant
}

export function clearSession() {
  accessToken = null
  currentTenant = null
  pendingRequest = null
}

/**
 * Pide un token para un tenant. El tenant que el usuario elige aqui solo decide con que
 * credenciales entra: quien manda es el claim que FIRMA el servidor. Cambiar el parametro no
 * cambia lo que la API deja leer.
 *
 * Varias llamadas concurrentes comparten la misma promesa en vuelo, para no disparar N
 * peticiones de token cuando la pantalla arranca.
 */
export async function ensureSession(tenantCode) {
  if (accessToken && currentTenant === tenantCode) return accessToken
  if (pendingRequest && currentTenant === tenantCode) return pendingRequest

  currentTenant = tenantCode
  pendingRequest = (async () => {
    const response = await fetch(`${API_BASE_URL}/session/token`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ tenantCode }),
    })

    const body = await response.json()
    if (!response.ok || body?.isSuccess === false) {
      clearSession()
      const error = new Error(body?.message ?? 'No fue posible iniciar sesion.')
      error.envelope = body
      throw error
    }

    accessToken = body.data.accessToken
    currentTenant = tenantCode
    pendingRequest = null
    return accessToken
  })()

  return pendingRequest
}

export async function fetchTenants() {
  const response = await fetch(`${API_BASE_URL}/session/tenants`)
  const body = await response.json()
  if (!response.ok || body?.isSuccess === false) {
    throw new Error(body?.message ?? 'No fue posible leer los tenants.')
  }
  return body.data
}

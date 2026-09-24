import { API_BASE_URL } from '../config/env'

/**
 * La sesion del ejemplo, y hay que decir lo que es: NO hay usuarios ni contraseñas. Se le pide un
 * token de prueba al propio backend y se guarda en memoria.
 *
 * En un sistema real el token lo emite el servicio de identidad y este archivo se sustituye
 * entero. Se guarda en MEMORIA y no en localStorage a proposito: un token en localStorage
 * sobrevive al cierre del navegador y lo puede leer cualquier script de la pagina.
 */
let accessToken = null
let grantedPermissions = []

export function getAccessToken() {
  return accessToken
}

export function getPermissions() {
  return grantedPermissions
}

export function hasPermission(permission) {
  return grantedPermissions.includes(permission)
}

/** Pide un token con los permisos indicados. Omitirlos = todos. */
export async function signIn(permissions) {
  const query = permissions?.length ? `?permissions=${encodeURIComponent(permissions.join(','))}` : ''
  const response = await fetch(`${API_BASE_URL}/api/dev/token${query}`, { method: 'POST' })

  if (!response.ok) {
    throw new Error('No se pudo obtener un token de prueba. Revisa que el backend este corriendo.')
  }

  const payload = await response.json()
  accessToken = payload.accessToken
  grantedPermissions = payload.permissions ?? []
  return grantedPermissions
}

export const PERMISSIONS = {
  print: 'printing:print',
  manageTemplates: 'printing:templates:manage',
  manageQueue: 'printing:queue:manage',
}

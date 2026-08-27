import { API_BASE_URL } from '../config/env'
import { getAccessToken } from '../auth/session'

/**
 * Cliente HTTP central. Todo el acceso a la API pasa por aqui: ningun componente ni hook llama
 * a fetch por su cuenta. Aqui se adjunta el token y aqui -y solo aqui- se desenvuelve el
 * envelope { data, isSuccess, message, utcTimeStamp }.
 */
async function request(path, { params, method = 'GET', body, signal } = {}) {
  const url = new URL(`${API_BASE_URL}${path}`, window.location.origin)
  if (params) {
    for (const [key, value] of Object.entries(params)) {
      if (value === undefined || value === null || value === '') continue
      url.searchParams.set(key, String(value))
    }
  }

  const headers = { Accept: 'application/json' }
  const token = getAccessToken()
  if (token) headers.Authorization = `Bearer ${token}`
  if (body !== undefined) headers['Content-Type'] = 'application/json'

  const response = await fetch(url, {
    method,
    headers,
    signal,
    body: body === undefined ? undefined : JSON.stringify(body),
  })

  let payload = null
  try {
    payload = await response.json()
  } catch {
    payload = null
  }

  return resolveApiEnvelope(response, payload)
}

/**
 * Desenvuelve el envelope. Un fallo de negocio esperado llega como HTTP 200 con
 * isSuccess:false, asi que 200 NO significa exito: hay que leer isSuccess.
 */
export function resolveApiEnvelope(response, payload) {
  if (payload && payload.isSuccess === true) return payload.data

  const error = new Error(extractApiErrorMessage(response, payload))
  error.envelope = payload
  error.status = response?.status
  throw error
}

/** Prioridad: el message del servidor; solo si no existe, un texto generico. */
export function extractApiErrorMessage(response, payload) {
  if (payload?.message) return payload.message
  if (response && !response.ok) return `Error ${response.status}`
  return 'Ocurrio un error inesperado.'
}

export const http = {
  get: (path, options) => request(path, { ...options, method: 'GET' }),
  post: (path, body, options) => request(path, { ...options, method: 'POST', body }),
}

import { API_BASE_URL } from '../config/env'
import { getAccessToken } from '../auth/session'

/**
 * Cliente HTTP central. Todo el acceso a la API pasa por aqui: ningun componente ni hook llama a
 * fetch por su cuenta. Aqui -y solo aqui- se desenvuelve el envelope de la API.
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

  if (payload && payload.isSuccess === true) return payload.data

  // El mensaje del servidor manda. Un texto generico del tipo "ocurrio un error" borra justo lo
  // unico util que trae la respuesta: POR QUE fallo.
  const message =
    payload?.message?.trim() ||
    `La peticion fallo con codigo ${response.status}.`
  throw new ApiError(message, response.status)
}

export class ApiError extends Error {
  constructor(message, status) {
    super(message)
    this.name = 'ApiError'
    this.status = status
  }
}

export const api = {
  get: (path, options) => request(path, { ...options, method: 'GET' }),
  post: (path, body, options) => request(path, { ...options, method: 'POST', body }),
  put: (path, body, options) => request(path, { ...options, method: 'PUT', body }),
  delete: (path, options) => request(path, { ...options, method: 'DELETE' }),
}

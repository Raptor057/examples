import { http } from './clients'

// Servicios del feature: una funcion por endpoint, sin logica de pantalla. Usan el cliente
// central, que es donde vive el desempaquetado del envelope.

export function loadMyAccess(options) {
  return http.get('/access/me', options)
}

export function loadUsers({ search, onlyActive, pageNumber, pageSize }, options) {
  return http.get('/access/users', { ...options, params: { search, onlyActive, pageNumber, pageSize } })
}

/**
 * El motivo viaja en el cuerpo porque lo escribe la persona. Quien la ejecuta NO viaja: eso sale
 * del token, y si viajara aqui cualquiera podria firmar la accion con el nombre de otro.
 */
export function deactivateUser(publicId, reason, options) {
  return http.post(`/access/users/${publicId}/deactivate`, { reason }, options)
}

export function loadRoles(options) {
  return http.get('/access/roles', options)
}

export function setRolePermission(rolePublicId, permissionCode, granted, reason, options) {
  return http.post(`/access/roles/${rolePublicId}/permissions`, { permissionCode, granted, reason }, options)
}

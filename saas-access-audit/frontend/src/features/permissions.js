/**
 * Los codigos de permiso que esta interfaz conoce.
 *
 * ES UN ESPEJO del catalogo que vive en el backend (Access.Contracts.PermissionCatalog), y el
 * espejo no manda: si un codigo de aqui no existe alla, el endpoint responde 403 y la opcion
 * simplemente no se usa nunca. Al reves tambien pasa: un permiso nuevo en el backend no aparece
 * en el menu hasta que alguien lo agregue aqui.
 *
 * Se escribe una vez, en un solo archivo, por la misma razon que en el backend se usa la
 * constante y no el literal: un typo repartido por diez componentes se ve exactamente igual que
 * "esta persona no tiene el permiso".
 */
export const PERMISSIONS = {
  usersView: 'access:user:view',
  usersDeactivate: 'access:user:deactivate',
  rolesView: 'access:role:view',
  rolesManage: 'access:role:manage',
  auditLogView: 'audit:log:view',
}

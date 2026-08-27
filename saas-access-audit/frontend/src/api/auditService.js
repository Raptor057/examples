import { http } from './clients'

// Las fechas viajan en UTC. El frontend es la UNICA capa que convierte a la hora local de quien
// mira, y lo hace en los dos sentidos: pinta en local y manda en UTC.

export function loadActionAudit(filters, options) {
  return http.get('/audit/actions', {
    ...options,
    params: {
      fromUtc: filters.fromUtc,
      toUtc: filters.toUtc,
      actionCode: filters.actionCode,
      subjectKey: filters.subjectKey,
      performedBy: filters.performedBy,
      onlyPendingExternalEffect: filters.onlyPendingExternalEffect,
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
    },
  })
}

export function loadAccessDenials(filters, options) {
  return http.get('/audit/denials', {
    ...options,
    params: {
      fromUtc: filters.fromUtc,
      toUtc: filters.toUtc,
      permissionCode: filters.permissionCode,
      attemptedBy: filters.attemptedBy,
      onlyWouldHaveBeenBlocked: filters.onlyWouldHaveBeenBlocked,
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
    },
  })
}

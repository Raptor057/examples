import { useCallback, useEffect, useState } from 'react'
import { loadActionAudit, loadAccessDenials } from '../../../api/auditService'
import { DEFAULT_PAGE_SIZE } from '../../../config/env'

const EMPTY_FILTERS = {
  fromUtc: '',
  toUtc: '',
  actionCode: '',
  permissionCode: '',
  subjectKey: '',
  performedBy: '',
  attemptedBy: '',
  onlyPendingExternalEffect: false,
  onlyWouldHaveBeenBlocked: false,
  pageNumber: 1,
  pageSize: DEFAULT_PAGE_SIZE,
}

/**
 * Convierte lo que se escribio en el campo de fecha -que el navegador da en hora LOCAL- a UTC,
 * que es como viaja y como esta guardado. El frontend es la unica capa que traduce entre las dos,
 * y lo hace en los dos sentidos: pinta en local y manda en UTC.
 */
function toUtcIso(localValue) {
  if (!localValue) return undefined
  const date = new Date(localValue)
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString()
}

/**
 * Estado de servidor de la bitacora, para las dos tablas.
 *
 * Todo se resuelve en el servidor: los filtros, el orden y la pagina. Con ciento cincuenta mil
 * renglones no es una preferencia -filtrar en el cliente solo podria mirar los 25 que le tocaron
 * a la pagina, y devolveria una lista vacia sin decir por que.
 */
export function useAuditLog(tab) {
  const [filters, setFilters] = useState(EMPTY_FILTERS)
  const [page, setPage] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  useEffect(() => {
    const controller = new AbortController()
    let active = true

    const request = {
      fromUtc: toUtcIso(filters.fromUtc),
      toUtc: toUtcIso(filters.toUtc),
      actionCode: filters.actionCode || undefined,
      permissionCode: filters.permissionCode || undefined,
      subjectKey: filters.subjectKey || undefined,
      performedBy: filters.performedBy || undefined,
      attemptedBy: filters.attemptedBy || undefined,
      onlyPendingExternalEffect: filters.onlyPendingExternalEffect,
      onlyWouldHaveBeenBlocked: filters.onlyWouldHaveBeenBlocked,
      pageNumber: filters.pageNumber,
      pageSize: filters.pageSize,
    }

    setLoading(true)
    setError(null)

    const promise =
      tab === 'actions'
        ? loadActionAudit(request, { signal: controller.signal })
        : loadAccessDenials(request, { signal: controller.signal })

    promise
      .then(({ data }) => {
        if (active) setPage(data)
      })
      .catch((cause) => {
        if (!active || cause.name === 'AbortError') return
        setError(cause.message)
      })
      .finally(() => {
        if (active) setLoading(false)
      })

    return () => {
      active = false
      controller.abort()
    }
  }, [tab, filters])

  // Cualquier cambio de filtro vuelve a la pagina 1. Sin esto, quien esta en la pagina 200 y
  // aplica un filtro ve una pantalla vacia y concluye que no hay resultados.
  const update = useCallback((patch) => {
    setFilters((previous) => ({ ...previous, ...patch, pageNumber: patch.pageNumber ?? 1 }))
  }, [])

  const clear = useCallback(() => setFilters(EMPTY_FILTERS), [])

  return { filters, page, loading, error, update, clear }
}

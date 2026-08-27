import { useCallback, useEffect, useState } from 'react'
import { loadUsers } from '../../../api/accessService'
import { DEFAULT_PAGE_SIZE } from '../../../config/env'

/**
 * Estado de servidor de la lista de usuarios.
 *
 * FILTRAR, ORDENAR Y PAGINAR SON LA MISMA OPERACION y se resuelven en el servidor. Este hook no
 * guarda una lista completa para filtrarla despues: cada cambio de filtro es una peticion nueva.
 * Filtrar en el cliente sobre datos paginados no da resultados lentos, da resultados
 * INCORRECTOS: el cliente solo puede filtrar lo que le toco en la pagina.
 *
 * Cambiar cualquier filtro devuelve a la pagina 1. Sin eso, quien esta en la pagina 40 y escribe
 * una busqueda ve una pantalla vacia y concluye que no hay resultados.
 */
export function useUsers() {
  const [query, setQuery] = useState({
    search: '',
    onlyActive: false,
    pageNumber: 1,
    pageSize: DEFAULT_PAGE_SIZE,
  })

  const [page, setPage] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [reloadToken, setReloadToken] = useState(0)

  useEffect(() => {
    const controller = new AbortController()
    let active = true

    setLoading(true)
    setError(null)

    loadUsers(query, { signal: controller.signal })
      .then(({ data }) => {
        if (active) setPage(data)
      })
      .catch((cause) => {
        // Cancelar no es un error: si la peticion se aborto porque llego otra, pintar rojo seria
        // reportar un fallo que nadie tuvo.
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
  }, [query, reloadToken])

  const update = useCallback((patch) => {
    setQuery((previous) => ({
      ...previous,
      ...patch,
      pageNumber: patch.pageNumber ?? 1,
    }))
  }, [])

  const reload = useCallback(() => setReloadToken((token) => token + 1), [])

  return { query, page, loading, error, update, reload }
}

import { useCallback, useEffect, useRef, useState } from 'react'
import { getOrdersTreeChildren } from '../../../api/ordersTreeService'
import { ORDERS_PAGE_SIZE } from '../../../config/env'
import { useI18n } from '../../../i18n'

// El arbol NUNCA se guarda como arbol. Un objeto anidado obliga a clonar el camino entero para
// tocar un nodo, y cada expansion redibuja ramas que no cambiaron.
const EMPTY_STORE = { nodesById: {}, childIdsById: {}, pageStateById: {} }

const ROOT_LEVEL = 'tree-year'

/**
 * Aplana en profundidad. Sirve igual para la raiz, para una expansion y para la hoja, que llega
 * con su subarbol ya armado: por eso es recursiva.
 */
function ingestItems(items, nodesByIdDraft, childIdsByIdDraft) {
  const ids = []
  for (const item of items) {
    nodesByIdDraft[item.id] = item
    ids.push(item.id)
    if (Array.isArray(item.children) && item.children.length > 0) {
      childIdsByIdDraft[item.id] = ingestItems(item.children, nodesByIdDraft, childIdsByIdDraft)
    }
  }
  return ids
}

/** Las coordenadas viajan tal cual salieron del servidor. El cliente no reconstruye ninguna. */
function coordinatesOf(node) {
  return {
    year: node.year,
    month: node.month,
    categoryId: node.categoryId,
    productId: node.productId,
    orderId: node.orderId,
  }
}

export default function useLazyOrdersTree({ tenantCode, searchType, searchValue, ready }) {
  const { t } = useI18n()

  // El traductor va por referencia y NO entra en las dependencias del efecto. Si entrara,
  // cambiar de idioma reiniciaria el store y el usuario perderia el arbol que ya abrio: el
  // idioma no es un parametro de la busqueda.
  const tRef = useRef(t)
  tRef.current = t

  const [store, setStore] = useState(EMPTY_STORE)
  const [rootIds, setRootIds] = useState([])
  const [expandedIds, setExpandedIds] = useState(() => new Set())
  // Estado POR NODO: una rama que falla no tumba el arbol, muestra su error en su renglon y las
  // demas siguen navegables. Un spinner global aqui es una regresion de producto.
  const [loadingIds, setLoadingIds] = useState(() => new Set())
  const [loadingMoreIds, setLoadingMoreIds] = useState(() => new Set())
  const [errorById, setErrorById] = useState({})
  const [selectedId, setSelectedId] = useState('')
  const [loadingRoot, setLoadingRoot] = useState(false)
  const [rootError, setRootError] = useState('')

  // La cache vive lo que vive la busqueda: al cambiar los parametros, el store se reinicia
  // entero. Colapsar, en cambio, NO descarta nada.
  useEffect(() => {
    setStore(EMPTY_STORE)
    setRootIds([])
    setExpandedIds(new Set())
    setLoadingIds(new Set())
    setLoadingMoreIds(new Set())
    setErrorById({})
    setSelectedId('')
    setRootError('')

    if (!ready) {
      setLoadingRoot(false)
      return undefined
    }

    if (searchType !== 'all' && !searchValue) {
      setLoadingRoot(false)
      return undefined
    }

    const controller = new AbortController()
    // Guardia de cancelacion: si el usuario cambia la busqueda mientras carga la raiz, la
    // respuesta vieja puede llegar despues y pisar la nueva.
    let active = true

    ;(async () => {
      setLoadingRoot(true)
      try {
        const result = await getOrdersTreeChildren(
          { level: ROOT_LEVEL, searchType, searchValue },
          { signal: controller.signal },
        )
        if (!active) return

        const nodesById = {}
        const childIdsById = {}
        const ids = ingestItems(result.items, nodesById, childIdsById)
        setStore({ nodesById, childIdsById, pageStateById: {} })
        setRootIds(ids)
      } catch (error) {
        if (!active || error?.name === 'AbortError') return
        setStore(EMPTY_STORE)
        setRootIds([])
        setRootError(error?.message || tRef.current('errors.rootLoad'))
      } finally {
        if (active) setLoadingRoot(false)
      }
    })()

    return () => {
      active = false
      controller.abort()
    }
  }, [tenantCode, searchType, searchValue, ready])

  const getNode = useCallback((id) => store.nodesById[id] ?? null, [store.nodesById])
  const getChildIds = useCallback((id) => store.childIdsById[id], [store.childIdsById])
  const getPageState = useCallback((id) => store.pageStateById[id], [store.pageStateById])
  const isExpanded = useCallback((id) => expandedIds.has(id), [expandedIds])
  const isLoading = useCallback((id) => loadingIds.has(id), [loadingIds])
  const isLoadingMore = useCallback((id) => loadingMoreIds.has(id), [loadingMoreIds])
  const getError = useCallback((id) => errorById[id] ?? '', [errorById])

  const isExpandable = useCallback(
    (id) => {
      const node = store.nodesById[id]
      if (!node) return false
      // nextLevel null significa hoja: el cliente no dibuja flecha sin preguntarle a nadie.
      if (node.nextLevel) return true
      const children = store.childIdsById[id]
      return Array.isArray(children) && children.length > 0
    },
    [store.nodesById, store.childIdsById],
  )

  const toggle = useCallback(
    async (id) => {
      const node = store.nodesById[id]
      if (!node || loadingIds.has(id)) return

      if (expandedIds.has(id)) {
        setExpandedIds((previous) => {
          const next = new Set(previous)
          next.delete(id)
          return next
        })
        return
      }

      // childIdsById[id] === undefined es "sin cargar"; === [] es "cargado y vacio".
      // Confundirlas produce el nodo que gira para siempre, o el que vuelve a pedir al
      // servidor cada vez que se abre.
      const alreadyLoaded = store.childIdsById[id] !== undefined
      if (alreadyLoaded || !node.nextLevel) {
        if (isExpandable(id)) setExpandedIds((previous) => new Set(previous).add(id))
        return
      }

      setLoadingIds((previous) => new Set(previous).add(id))
      setErrorById((previous) => {
        if (!previous[id]) return previous
        const next = { ...previous }
        delete next[id]
        return next
      })

      try {
        const result = await getOrdersTreeChildren({
          level: node.nextLevel,
          searchType,
          searchValue,
          ...coordinatesOf(node),
          pageNumber: 1,
          pageSize: ORDERS_PAGE_SIZE,
        })

        setStore((previous) => {
          const nodesById = { ...previous.nodesById }
          const childIdsById = { ...previous.childIdsById }
          const ids = ingestItems(result.items, nodesById, childIdsById)
          childIdsById[id] = ids
          return {
            nodesById,
            childIdsById,
            pageStateById: {
              ...previous.pageStateById,
              [id]: {
                level: node.nextLevel,
                pageNumber: result.pageNumber,
                pageSize: result.pageSize,
                totalCount: result.totalCount,
                hasMore: Boolean(result.hasMore),
              },
            },
          }
        })

        setExpandedIds((previous) => new Set(previous).add(id))
      } catch (error) {
        setErrorById((previous) => ({ ...previous, [id]: error?.message || tRef.current('errors.childrenLoad') }))
      } finally {
        setLoadingIds((previous) => {
          const next = new Set(previous)
          next.delete(id)
          return next
        })
      }
    },
    [store, expandedIds, loadingIds, isExpandable, searchType, searchValue],
  )

  /** Siguiente pagina del unico nivel que pagina. El cliente no sabe cual es: lo dice hasMore. */
  const loadMore = useCallback(
    async (id) => {
      const node = store.nodesById[id]
      const pageState = store.pageStateById[id]
      if (!node || !pageState?.hasMore || loadingMoreIds.has(id)) return

      setLoadingMoreIds((previous) => new Set(previous).add(id))
      try {
        const result = await getOrdersTreeChildren({
          level: pageState.level,
          searchType,
          searchValue,
          ...coordinatesOf(node),
          pageNumber: pageState.pageNumber + 1,
          pageSize: pageState.pageSize,
        })

        setStore((previous) => {
          const nodesById = { ...previous.nodesById }
          const childIdsById = { ...previous.childIdsById }
          const newIds = ingestItems(result.items, nodesById, childIdsById)

          // Dedup: si entre la pagina 1 y la 2 entraron datos nuevos, el paginado puede repetir
          // un id. Sin filtrar, React avisa de keys duplicadas y el renglon se dibuja dos veces.
          const existing = new Set(previous.childIdsById[id] ?? [])
          const uniqueNewIds = newIds.filter((newId) => !existing.has(newId))
          childIdsById[id] = [...(previous.childIdsById[id] ?? []), ...uniqueNewIds]

          return {
            nodesById,
            childIdsById,
            pageStateById: {
              ...previous.pageStateById,
              [id]: {
                ...pageState,
                pageNumber: result.pageNumber,
                totalCount: result.totalCount || pageState.totalCount,
                hasMore: Boolean(result.hasMore),
              },
            },
          }
        })
      } catch (error) {
        setErrorById((previous) => ({ ...previous, [id]: error?.message || tRef.current('errors.childrenLoad') }))
      } finally {
        setLoadingMoreIds((previous) => {
          const next = new Set(previous)
          next.delete(id)
          return next
        })
      }
    },
    [store, loadingMoreIds, searchType, searchValue],
  )

  return {
    rootIds,
    loadingRoot,
    rootError,
    selectedId,
    setSelectedId,
    selectedNode: selectedId ? (store.nodesById[selectedId] ?? null) : null,
    getNode,
    getChildIds,
    getPageState,
    getError,
    isExpanded,
    isExpandable,
    isLoading,
    isLoadingMore,
    toggle,
    loadMore,
  }
}

import { useCallback, useRef, useState } from 'react'
import { getOrdersTreeExport } from '../../../api/ordersTreeService'
import { EXPORT_BATCH_SIZE, EXPORT_PAGE_SIZE } from '../../../config/env'
import { useI18n } from '../../../i18n'
import { downloadCsvChunks, rowsToCsvChunk } from '../utils/csvChunks'

/**
 * Descarga acotada al nodo, por paginas y en bloques de texto.
 *
 * Tres cosas que no son opcionales:
 *   - el total se pide SOLO en la primera pagina y alimenta la barra de progreso,
 *   - cada pagina se textualiza al vuelo y se acumula como bloque,
 *   - cancelar NO es un error: si el usuario aborto a proposito, no se pinta nada en rojo.
 */
export default function useOrdersTreeExport({ searchType, searchValue }) {
  const { t, locale, formatDateTime } = useI18n()
  const [progress, setProgress] = useState(null)
  const [message, setMessage] = useState('')
  const [cancelling, setCancelling] = useState(false)
  const abortRef = useRef(null)

  const cancel = useCallback(() => {
    setCancelling(true)
    abortRef.current?.abort()
  }, [])

  const run = useCallback(
    async (scope, fileBase) => {
      if (progress || !scope) return

      setMessage('')
      setCancelling(false)
      const controller = new AbortController()
      abortRef.current = controller

      try {
        const baseParams = { searchType, searchValue, ...scope, pageSize: EXPORT_PAGE_SIZE }

        setProgress({
          phase: 'preparing',
          completedPages: 0,
          totalPages: 0,
          exportedRows: 0,
          totalCount: 0,
          statusMessage: t('export.preparing'),
        })

        const first = await getOrdersTreeExport(
          { ...baseParams, pageNumber: 1 },
          { signal: controller.signal },
        )

        const totalCount = Number(first.totalCount ?? first.items.length)
        const totalPages = Math.max(1, Math.ceil(totalCount / EXPORT_PAGE_SIZE))

        // El encabezado se traduce AQUI, en el idioma activo, igual que las columnas de la
        // pantalla. El servidor manda llaves, no titulos.
        const headers = first.columns.map((column) => t(`column.${column.key}`))
        const chunks = [rowsToCsvChunk([headers])]
        let exportedRows = 0

        const appendPage = (page) => {
          if (!page.items.length) return
          chunks.push(rowsToCsvChunk(page.items.map((row) => formatRow(row, first.columns))))
          exportedRows += page.items.length
        }

        appendPage(first)

        setProgress({
          phase: totalPages > 1 ? 'fetching' : 'building',
          completedPages: 1,
          totalPages,
          exportedRows,
          totalCount,
          statusMessage:
            totalPages > 1 ? t('export.fetching', { completed: 1, total: totalPages }) : t('export.building'),
        })

        if (totalPages > 1) {
          const remaining = Array.from({ length: totalPages - 1 }, (unused, index) => index + 2)
          let completedPages = 1

          for (let index = 0; index < remaining.length; index += EXPORT_BATCH_SIZE) {
            if (controller.signal.aborted) throw new DOMException('cancelled', 'AbortError')

            const batch = remaining.slice(index, index + EXPORT_BATCH_SIZE)
            const pages = await Promise.all(
              batch.map((pageNumber) =>
                getOrdersTreeExport({ ...baseParams, pageNumber }, { signal: controller.signal }),
              ),
            )

            pages.forEach(appendPage)
            completedPages += batch.length

            setProgress({
              phase: completedPages >= totalPages ? 'building' : 'fetching',
              completedPages,
              totalPages,
              exportedRows,
              totalCount,
              statusMessage:
                completedPages >= totalPages
                  ? t('export.building')
                  : t('export.fetching', { completed: completedPages, total: totalPages }),
            })
          }
        }

        if (!exportedRows) {
          setMessage(t('export.empty'))
          return
        }

        downloadCsvChunks(chunks, fileBase)
      } catch (error) {
        const cancelled = error?.name === 'AbortError'
        if (!cancelled) setMessage(error?.message || t('errors.export'))
      } finally {
        abortRef.current = null
        setCancelling(false)
        setProgress(null)
      }
    },
    [progress, searchType, searchValue, t, locale, formatDateTime],
  )

  /**
   * Los numeros y las fechas salen del archivo con el MISMO formato que el usuario ve en la
   * pantalla. Por eso el servidor manda valores y no texto: el formato depende del idioma.
   */
  function formatRow(row, columns) {
    return row.map((value, index) => {
      const type = columns[index]?.type
      if (value === null || value === undefined) return ''
      if (type === 'dateTimeUtc') return formatDateTime(value)
      if (type === 'money' || type === 'integer') return new Intl.NumberFormat(locale).format(Number(value))
      return value
    })
  }

  return { progress, message, cancelling, cancel, run, exporting: progress != null }
}

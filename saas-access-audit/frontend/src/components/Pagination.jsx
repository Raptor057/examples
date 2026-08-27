import { useI18n } from '../i18n'
import { PAGE_SIZE_OPTIONS } from '../config/env'
import { ui } from '../styles/designSystem'

/**
 * Paginador de servidor.
 *
 * El total que muestra es el del CONJUNTO FILTRADO COMPLETO, no el de la pagina: viene del
 * COUNT(1) OVER() de la misma consulta. Es la diferencia entre "hay 150 mil renglones y estas
 * viendo 25" y "hay 25", que es lo unico que podria decir una pantalla que filtra en el cliente.
 */
export default function Pagination({ pageNumber, pageSize, totalCount, loading, onPageChange, onPageSizeChange }) {
  const { t, tCount, formatNumber } = useI18n()
  const lastPage = Math.max(1, Math.ceil(totalCount / pageSize))

  return (
    <div className={ui.layout.footerBar}>
      <p className={ui.typography.hint} aria-live="polite">
        {tCount('common.rowCount', totalCount)}
        {' · '}
        {t('common.page', { page: `${formatNumber(pageNumber)} / ${formatNumber(lastPage)}` })}
      </p>

      <div className="flex flex-wrap items-center gap-2">
        <label className={ui.typography.hint} htmlFor="page-size">
          {t('common.pageSize')}
        </label>
        <select
          id="page-size"
          className="h-9 rounded-lg border border-slate-300 bg-white px-2 text-sm"
          value={pageSize}
          onChange={(event) => onPageSizeChange(Number(event.target.value))}
        >
          {PAGE_SIZE_OPTIONS.map((size) => (
            <option key={size} value={size}>
              {size}
            </option>
          ))}
        </select>

        <button
          type="button"
          className={ui.controls.secondaryButton}
          disabled={loading || pageNumber <= 1}
          onClick={() => onPageChange(pageNumber - 1)}
        >
          {t('common.previous')}
        </button>
        <button
          type="button"
          className={ui.controls.secondaryButton}
          disabled={loading || pageNumber >= lastPage}
          onClick={() => onPageChange(pageNumber + 1)}
        >
          {t('common.next')}
        </button>
      </div>
    </div>
  )
}

import { ui } from '../../../styles/designSystem'
import { useI18n } from '../../../i18n'

/**
 * Progreso por FASES y cancelacion de verdad. Una descarga larga sin senal se lee como pantalla
 * colgada, y el usuario recarga a la mitad o vuelve a picar el boton.
 */
export default function ExportProgress({ progress, onCancel, cancelling }) {
  const { t, formatNumber } = useI18n()
  if (!progress) return null

  const percent =
    progress.totalPages > 0 ? Math.min(100, Math.round((progress.completedPages / progress.totalPages) * 100)) : 0

  return (
    <div className={ui.overlay.backdrop} role="dialog" aria-modal="true" aria-labelledby="export-progress-title">
      <div className={ui.overlay.panel}>
        <h2 id="export-progress-title" className={ui.typography.sectionTitle}>
          {t('export.title')}
        </h2>

        {/* aria-live para que un lector de pantalla anuncie el avance sin robar el foco. */}
        <p className={`${ui.typography.body} mt-2`} aria-live="polite">
          {progress.statusMessage}
        </p>

        <div className={ui.overlay.progressTrack} role="progressbar" aria-valuenow={percent} aria-valuemin={0} aria-valuemax={100}>
          <div className={ui.overlay.progressBar} style={{ width: `${percent}%` }} />
        </div>

        <p className={`${ui.typography.hint} mt-2`}>
          {t('export.rows', {
            exported: formatNumber(progress.exportedRows),
            total: formatNumber(progress.totalCount),
          })}
        </p>

        <button type="button" className={`${ui.controls.secondaryButton} mt-4 w-full`} onClick={onCancel} disabled={cancelling}>
          {t('common.cancel')}
        </button>
      </div>
    </div>
  )
}

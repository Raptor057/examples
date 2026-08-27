import { ui } from '../styles/designSystem'

const TONES = {
  error: ui.feedback.errorBanner,
  warning: ui.feedback.warningBanner,
  info: ui.feedback.infoBanner,
  success: ui.feedback.successBanner,
}

/**
 * Aviso en linea. Los errores llevan role="alert" para que el lector de pantalla los anuncie sin
 * que nadie tenga que ir a buscarlos; el resto usa aria-live cortes, que no interrumpe.
 *
 * El tono no es decorado: un exito PARCIAL -la accion ocurrio pero su efecto externo quedo
 * pendiente- se pinta como advertencia, no como exito. Pintarlo verde seria mentir.
 */
export default function Banner({ tone = 'info', title, children, onDismiss, dismissLabel }) {
  return (
    <div
      className={TONES[tone] ?? TONES.info}
      role={tone === 'error' ? 'alert' : 'status'}
      aria-live={tone === 'error' ? 'assertive' : 'polite'}
    >
      <div className="flex items-start justify-between gap-3">
        <div>
          {title ? <p className="font-semibold">{title}</p> : null}
          <div>{children}</div>
        </div>
        {onDismiss ? (
          <button type="button" className={ui.controls.linkButton} onClick={onDismiss}>
            {dismissLabel}
          </button>
        ) : null}
      </div>
    </div>
  )
}

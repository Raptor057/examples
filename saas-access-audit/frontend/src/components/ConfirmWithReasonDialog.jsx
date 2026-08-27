import { useEffect, useId, useRef, useState } from 'react'
import { useI18n } from '../i18n'
import { ui } from '../styles/designSystem'

const MIN_REASON = 5

/**
 * EL DIALOGO DE UNA ACCION QUE NO SE DESHACE.
 *
 * Dice tres cosas y no sobra ninguna: sobre QUE objeto, QUE va a pasar en terminos del usuario, y
 * que NO se puede deshacer. El boton de confirmar lleva el VERBO de la accion -"Desactivar",
 * "Revocar"- y nunca "Aceptar": es lo ultimo que se lee antes de pulsar.
 *
 * Y pide MOTIVO, que no es un tramite: es la columna que la bitacora existe para responder meses
 * despues. Por eso el motivo se escribe aqui, viaja en la peticion y el servidor lo valida otra
 * vez -y la base lo defiende con un CHECK-, en vez de confiar en este formulario.
 *
 * Lo que este dialogo NO hace es autorizar. Lo pulsa cualquiera que haya llegado a la pantalla y
 * no deja rastro por si mismo. La autorizacion y el registro son del servidor.
 */
export default function ConfirmWithReasonDialog({
  open,
  title,
  body,
  confirmLabel,
  danger = false,
  busy = false,
  error,
  onConfirm,
  onCancel,
}) {
  const { t } = useI18n()
  const [reason, setReason] = useState('')
  const [touched, setTouched] = useState(false)
  const titleId = useId()
  const reasonId = useId()
  const errorId = useId()
  const textareaRef = useRef(null)
  const previousFocusRef = useRef(null)

  useEffect(() => {
    if (!open) return undefined

    // El foco entra al abrir y VUELVE al disparador al cerrar: sin eso, quien navega con teclado
    // termina al principio de la pagina cada vez que confirma algo.
    previousFocusRef.current = document.activeElement
    setReason('')
    setTouched(false)
    textareaRef.current?.focus()

    const onKeyDown = (event) => {
      if (event.key === 'Escape' && !busy) onCancel()
    }

    document.addEventListener('keydown', onKeyDown)
    return () => {
      document.removeEventListener('keydown', onKeyDown)
      previousFocusRef.current?.focus?.()
    }
  }, [open, busy, onCancel])

  if (!open) return null

  const trimmed = reason.trim()
  const invalid = trimmed.length < MIN_REASON
  const showError = touched && invalid

  return (
    <div className={ui.overlay.backdrop}>
      <div className={ui.overlay.panel} role="dialog" aria-modal="true" aria-labelledby={titleId}>
        <h2 id={titleId} className={ui.typography.pageTitle}>
          {title}
        </h2>
        <p className={`${ui.typography.body} mt-2`}>{body}</p>

        <div className="mt-4">
          <label className={ui.controls.label} htmlFor={reasonId}>
            {t('common.reason')}
          </label>
          <textarea
            id={reasonId}
            ref={textareaRef}
            className={ui.controls.textarea}
            rows={3}
            value={reason}
            placeholder={t('common.reasonPlaceholder')}
            aria-invalid={showError}
            aria-describedby={showError ? errorId : undefined}
            onChange={(event) => setReason(event.target.value)}
            onBlur={() => setTouched(true)}
          />
          {showError ? (
            <p id={errorId} className={ui.feedback.inlineError}>
              {t('common.reasonRequired', { min: MIN_REASON })}
            </p>
          ) : null}
        </div>

        {error ? (
          <p className={`${ui.feedback.errorBanner} mt-3`} role="alert">
            {error}
          </p>
        ) : null}

        <div className="mt-5 flex flex-wrap justify-end gap-2">
          <button type="button" className={ui.controls.secondaryButton} onClick={onCancel} disabled={busy}>
            {t('common.cancel')}
          </button>
          <button
            type="button"
            className={danger ? ui.controls.dangerButton : ui.controls.primaryButton}
            disabled={busy}
            onClick={() => {
              setTouched(true)
              if (invalid) return
              onConfirm(trimmed)
            }}
          >
            {busy ? t('common.loading') : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  )
}

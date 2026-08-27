import { useId } from 'react'
import { useI18n } from '../../../i18n'
import { ui } from '../../../styles/designSystem'
import { PERMISSIONS } from '../../permissions'

const ACTION_CODES = [PERMISSIONS.usersDeactivate, PERMISSIONS.rolesManage]
const PERMISSION_CODES = Object.values(PERMISSIONS)

/**
 * Filtros de la bitacora. Todos viajan al servidor.
 *
 * Los codigos se ofrecen como LISTA CERRADA en vez de campo libre: son un conjunto conocido, y
 * escribirlos a mano solo produce busquedas vacias por un typo que nadie ve.
 */
export default function AuditToolbar({ tab, filters, loading, onChange, onClear }) {
  const { t } = useI18n()
  const fromId = useId()
  const toId = useId()
  const codeId = useId()
  const whoId = useId()
  const subjectId = useId()
  const flagId = useId()

  const isActions = tab === 'actions'

  return (
    <div className={ui.layout.toolbar}>
      <div className={ui.layout.field}>
        <label className={ui.controls.label} htmlFor={fromId}>{t('audit.from')}</label>
        <input
          id={fromId}
          type="datetime-local"
          className={ui.controls.input}
          value={filters.fromUtc}
          onChange={(event) => onChange({ fromUtc: event.target.value })}
        />
      </div>

      <div className={ui.layout.field}>
        <label className={ui.controls.label} htmlFor={toId}>{t('audit.to')}</label>
        <input
          id={toId}
          type="datetime-local"
          className={ui.controls.input}
          value={filters.toUtc}
          onChange={(event) => onChange({ toUtc: event.target.value })}
        />
      </div>

      <div className={ui.layout.field}>
        <label className={ui.controls.label} htmlFor={codeId}>
          {isActions ? t('audit.actionCode') : t('audit.permissionCode')}
        </label>
        <select
          id={codeId}
          className={ui.controls.select}
          value={isActions ? filters.actionCode : filters.permissionCode}
          onChange={(event) =>
            onChange(isActions ? { actionCode: event.target.value } : { permissionCode: event.target.value })
          }
        >
          <option value="">{isActions ? t('audit.anyAction') : t('audit.anyPermission')}</option>
          {(isActions ? ACTION_CODES : PERMISSION_CODES).map((code) => (
            <option key={code} value={code}>{code}</option>
          ))}
        </select>
      </div>

      <div className={ui.layout.field}>
        <label className={ui.controls.label} htmlFor={whoId}>
          {isActions ? t('audit.performedBy') : t('audit.attemptedBy')}
        </label>
        <input
          id={whoId}
          type="search"
          className={ui.controls.input}
          value={isActions ? filters.performedBy : filters.attemptedBy}
          onChange={(event) =>
            onChange(isActions ? { performedBy: event.target.value } : { attemptedBy: event.target.value })
          }
        />
      </div>

      {isActions ? (
        <div className={ui.layout.field}>
          <label className={ui.controls.label} htmlFor={subjectId}>{t('audit.subjectKey')}</label>
          <input
            id={subjectId}
            type="search"
            className={ui.controls.input}
            value={filters.subjectKey}
            onChange={(event) => onChange({ subjectKey: event.target.value })}
          />
        </div>
      ) : null}

      <label className={`${ui.controls.checkboxRow} h-11`} htmlFor={flagId}>
        <input
          id={flagId}
          type="checkbox"
          className={ui.controls.checkbox}
          checked={isActions ? filters.onlyPendingExternalEffect : filters.onlyWouldHaveBeenBlocked}
          onChange={(event) =>
            onChange(
              isActions
                ? { onlyPendingExternalEffect: event.target.checked }
                : { onlyWouldHaveBeenBlocked: event.target.checked },
            )
          }
        />
        {isActions ? t('audit.onlyPending') : t('audit.onlyWouldBlock')}
      </label>

      <button type="button" className={ui.controls.secondaryButton} onClick={onClear}>
        {t('common.clear')}
      </button>

      <span className={ui.typography.hint} aria-live="polite">
        {loading ? t('common.loading') : ''}
      </span>
    </div>
  )
}

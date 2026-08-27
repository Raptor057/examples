import { useEffect, useId, useState } from 'react'
import { useI18n } from '../../../i18n'
import { ui } from '../../../styles/designSystem'

/**
 * Barra de filtros. El texto se manda al servidor con retardo (debounce) para no disparar una
 * consulta por tecla; las casillas se mandan de inmediato, porque un clic ya es una decision.
 */
export default function UsersToolbar({ query, loading, onChange }) {
  const { t } = useI18n()
  const searchId = useId()
  const activeId = useId()
  const [draft, setDraft] = useState(query.search)

  useEffect(() => setDraft(query.search), [query.search])

  useEffect(() => {
    if (draft === query.search) return undefined
    const timer = setTimeout(() => onChange({ search: draft }), 350)
    return () => clearTimeout(timer)
  }, [draft, query.search, onChange])

  return (
    <div className={ui.layout.toolbar}>
      <div className={`${ui.layout.field} flex-1`}>
        <label className={ui.controls.label} htmlFor={searchId}>
          {t('users.searchLabel')}
        </label>
        <input
          id={searchId}
          type="search"
          className={ui.controls.input}
          value={draft}
          onChange={(event) => setDraft(event.target.value)}
        />
      </div>

      <label className={`${ui.controls.checkboxRow} h-11`} htmlFor={activeId}>
        <input
          id={activeId}
          type="checkbox"
          className={ui.controls.checkbox}
          checked={query.onlyActive}
          onChange={(event) => onChange({ onlyActive: event.target.checked })}
        />
        {t('users.onlyActive')}
      </label>

      <span className={ui.typography.hint} aria-live="polite">
        {loading ? t('common.loading') : ''}
      </span>
    </div>
  )
}

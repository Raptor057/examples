import { useId, useState } from 'react'
import { ui } from '../../../styles/designSystem'
import { useI18n } from '../../../i18n'

const SEARCH_TYPES = ['all', 'status', 'customer', 'ordernumber']

/**
 * Filtros del arbol. Aplicar cambia los parametros de la busqueda, y eso reinicia la cache
 * entera: los nodos ya cargados dejaron de ser validos.
 */
export default function OrdersTreeToolbar({ searchType, searchValue, onApply, disabled }) {
  const { t } = useI18n()
  const typeId = useId()
  const valueId = useId()

  const [draftType, setDraftType] = useState(searchType)
  const [draftValue, setDraftValue] = useState(searchValue)

  const needsValue = draftType !== 'all'

  function handleSubmit(event) {
    event.preventDefault()
    onApply({ searchType: draftType, searchValue: needsValue ? draftValue.trim() : '' })
  }

  return (
    <form className={ui.layout.toolbar} onSubmit={handleSubmit}>
      <div className="w-56">
        <label className={ui.controls.label} htmlFor={typeId}>
          {t('search.type')}
        </label>
        <select
          id={typeId}
          className={ui.controls.select}
          value={draftType}
          onChange={(event) => setDraftType(event.target.value)}
          disabled={disabled}
        >
          {SEARCH_TYPES.map((type) => (
            <option key={type} value={type}>
              {t(`search.type.${type}`)}
            </option>
          ))}
        </select>
      </div>

      <div className="w-64">
        <label className={ui.controls.label} htmlFor={valueId}>
          {t('search.value')}
        </label>
        <input
          id={valueId}
          className={ui.controls.input}
          value={draftValue}
          placeholder={t('search.valuePlaceholder')}
          onChange={(event) => setDraftValue(event.target.value)}
          disabled={disabled || !needsValue}
        />
      </div>

      <button type="submit" className={ui.controls.primaryButton} disabled={disabled || (needsValue && !draftValue.trim())}>
        {t('search.apply')}
      </button>
    </form>
  )
}

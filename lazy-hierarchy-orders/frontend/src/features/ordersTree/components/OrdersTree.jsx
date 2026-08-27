import { useCallback, useRef } from 'react'
import { ui } from '../../../styles/designSystem'
import { useI18n } from '../../../i18n'
import OrdersTreeNode from './OrdersTreeNode'
import Spinner from './Spinner'

export default function OrdersTree({ tree }) {
  const { t } = useI18n()
  const containerRef = useRef(null)

  // Flechas arriba y abajo mueven el foco al renglon visible anterior o siguiente. Se resuelve
  // sobre el DOM porque "visible" depende de que ramas estan expandidas en este momento.
  const moveFocus = useCallback((currentElement, delta) => {
    const container = containerRef.current
    if (!container) return
    const rows = Array.from(container.querySelectorAll('[data-treeitem-row]'))
    const index = rows.indexOf(currentElement)
    const next = rows[index + delta]
    if (next) next.focus()
  }, [])

  if (tree.loadingRoot) {
    return (
      <div className="flex items-center gap-2 px-1 py-8 text-sm text-slate-500">
        <Spinner /> {t('common.loading')}
      </div>
    )
  }

  if (tree.rootError) {
    return (
      <div role="alert" className={ui.feedback.errorBanner}>
        {tree.rootError}
      </div>
    )
  }

  if (tree.rootIds.length === 0) {
    return <p className={ui.feedback.empty}>{t('tree.empty')}</p>
  }

  return (
    <div ref={containerRef}>
      <ul role="tree" aria-label={t('tree.title')} className="list-none">
        {tree.rootIds.map((id) => (
          <OrdersTreeNode key={id} id={id} depth={0} tree={tree} onMoveFocus={moveFocus} />
        ))}
      </ul>
    </div>
  )
}

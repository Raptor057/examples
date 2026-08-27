import { nodeTone, ui } from '../../../styles/designSystem'
import { useI18n } from '../../../i18n'
import { nodeSubtitle, nodeTitle } from '../utils/nodeFormat'
import Spinner from './Spinner'

const INDENT_PX = 18

/**
 * Un renglon del arbol. Sigue el patron WAI-ARIA de treeview: role treeitem, aria-expanded solo
 * cuando el nodo se puede expandir, aria-level para que el lector anuncie la profundidad, y
 * teclado completo. Un arbol sin teclado no es navegable.
 */
export default function OrdersTreeNode({ id, depth, tree, onMoveFocus }) {
  const i18n = useI18n()
  const { t } = i18n

  const node = tree.getNode(id)
  if (!node) return null

  const expandable = tree.isExpandable(id)
  const expanded = tree.isExpanded(id)
  const loading = tree.isLoading(id)
  const loadingMore = tree.isLoadingMore(id)
  const error = tree.getError(id)
  const childIds = tree.getChildIds(id)
  const pageState = tree.getPageState(id)
  const selected = tree.selectedId === id

  function handleKeyDown(event) {
    switch (event.key) {
      case 'Enter':
      case ' ':
        event.preventDefault()
        tree.setSelectedId(id)
        break
      case 'ArrowRight':
        if (expandable && !expanded) {
          event.preventDefault()
          tree.toggle(id)
        }
        break
      case 'ArrowLeft':
        if (expandable && expanded) {
          event.preventDefault()
          tree.toggle(id)
        }
        break
      case 'ArrowDown':
        event.preventDefault()
        onMoveFocus(event.currentTarget, 1)
        break
      case 'ArrowUp':
        event.preventDefault()
        onMoveFocus(event.currentTarget, -1)
        break
      default:
        break
    }
  }

  const subtitle = nodeSubtitle(node, i18n)

  return (
    <li role="treeitem" aria-expanded={expandable ? expanded : undefined} aria-selected={selected} aria-level={depth + 1}>
      <div
        data-treeitem-row=""
        role="presentation"
        tabIndex={0}
        onClick={() => tree.setSelectedId(id)}
        onKeyDown={handleKeyDown}
        className={`${ui.tree.row} ${selected ? ui.tree.rowSelected : ''}`}
        style={{ paddingLeft: `${depth * INDENT_PX + 4}px` }}
      >
        {expandable ? (
          <button
            type="button"
            tabIndex={-1}
            onClick={(event) => {
              event.stopPropagation()
              tree.toggle(id)
            }}
            className={ui.tree.toggle}
            aria-label={expanded ? t('tree.collapse') : t('tree.expand')}
          >
            {loading ? (
              <Spinner />
            ) : (
              <svg className={`size-4 transition-transform ${expanded ? 'rotate-90' : ''}`} viewBox="0 0 20 20" fill="currentColor" aria-hidden="true">
                <path d="M7.5 5l5 5-5 5V5z" />
              </svg>
            )}
          </button>
        ) : (
          <span className="inline-block size-6 shrink-0" aria-hidden="true" />
        )}

        {/* El color es refuerzo, nunca la unica senal: el tipo tambien va en texto. */}
        <span className={`size-2 shrink-0 rounded-full ${nodeTone[node.nodeType] ?? 'bg-slate-300'}`} aria-hidden="true" />

        <span className="flex min-w-0 flex-1 items-baseline gap-2">
          <span className="shrink-0 text-[11px] uppercase tracking-wide text-slate-400">
            {t(`node.${node.nodeType}`)}
          </span>
          <span className={ui.tree.label}>{nodeTitle(node, i18n)}</span>
          {subtitle ? <span className={ui.tree.subLabel}>{subtitle}</span> : null}
        </span>
      </div>

      {expanded ? (
        <ul role="group" className="list-none">
          {error ? (
            <li role="none" className={ui.feedback.inlineError} style={{ paddingLeft: `${depth * INDENT_PX + 34}px` }}>
              <span role="alert">{error}</span>
            </li>
          ) : null}

          {Array.isArray(childIds) && childIds.length === 0 ? (
            <li role="none" className="py-1 text-xs text-slate-400" style={{ paddingLeft: `${depth * INDENT_PX + 34}px` }}>
              {t('tree.noChildren')}
            </li>
          ) : null}

          {Array.isArray(childIds)
            ? childIds.map((childId) => (
                <OrdersTreeNode key={childId} id={childId} depth={depth + 1} tree={tree} onMoveFocus={onMoveFocus} />
              ))
            : null}

          {pageState?.hasMore ? (
            <li role="none" className="flex items-center gap-3 py-2" style={{ paddingLeft: `${depth * INDENT_PX + 34}px` }}>
              <button
                type="button"
                className={ui.controls.secondaryButton}
                onClick={() => tree.loadMore(id)}
                disabled={loadingMore}
              >
                {loadingMore ? <Spinner /> : null}
                {t('tree.loadMore')}
              </button>
              <span className={ui.typography.hint}>
                {t('tree.showingOf', {
                  shown: i18n.formatNumber(childIds?.length ?? 0),
                  total: i18n.formatNumber(pageState.totalCount ?? 0),
                })}
              </span>
            </li>
          ) : null}
        </ul>
      ) : null}
    </li>
  )
}

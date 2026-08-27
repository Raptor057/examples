import { ui } from '../../../styles/designSystem'
import { useI18n } from '../../../i18n'
import { exportFileBase, exportScopeOf, nodeSubtitle, nodeTitle } from '../utils/nodeFormat'

const DETAIL_ORDER = ['code', 'sku', 'customerName', 'status', 'placedAtUtc', 'categoryName', 'focusUnitCount', 'focusAmount']

/**
 * Detalle del nodo seleccionado y punto desde el que se exporta. El usuario descarga lo que
 * esta viendo: el alcance sale de las coordenadas del nodo, sin pedirle un solo dato mas.
 */
export default function NodeDetails({ node, onExport, exporting, message }) {
  const i18n = useI18n()
  const { t, formatMoney, formatNumber, formatDateTime } = i18n

  if (!node) {
    return (
      <section aria-labelledby="details-title">
        <h2 id="details-title" className={ui.typography.sectionTitle}>
          {t('details.title')}
        </h2>
        <p className={`${ui.typography.body} mt-2`}>{t('details.empty')}</p>
      </section>
    )
  }

  const metrics = node.metrics ?? {}
  const details = node.details ?? {}
  const scope = exportScopeOf(node)
  const scopeText = describeScope(node, i18n)

  return (
    <section aria-labelledby="details-title">
      <h2 id="details-title" className={ui.typography.sectionTitle}>
        {t('details.title')}
      </h2>

      <p className={`${ui.typography.eyebrow} mt-3`}>{t(`node.${node.nodeType}`)}</p>
      <p className="mt-1 text-base font-semibold text-slate-900">{nodeTitle(node, i18n)}</p>
      <p className={ui.typography.hint}>{nodeSubtitle(node, i18n)}</p>

      <dl className="mt-4 divide-y divide-slate-100 border-t border-slate-100">
        {metrics.orderCount != null ? <Row label={t('details.orderCount')} value={formatNumber(metrics.orderCount)} /> : null}
        {metrics.lineCount != null ? <Row label={t('details.lineCount')} value={formatNumber(metrics.lineCount)} /> : null}
        {metrics.unitCount != null ? <Row label={t('details.unitCount')} value={formatNumber(metrics.unitCount)} /> : null}
        {metrics.unitPrice != null ? <Row label={t('details.unitPrice')} value={formatMoney(metrics.unitPrice)} /> : null}
        {metrics.totalAmount != null ? <Row label={t('details.totalAmount')} value={formatMoney(metrics.totalAmount)} /> : null}

        {DETAIL_ORDER.filter((key) => details[key] != null && details[key] !== '').map((key) => (
          <Row
            key={key}
            label={t(`details.${key}`)}
            value={
              key === 'placedAtUtc'
                ? formatDateTime(details[key])
                : key === 'focusAmount'
                  ? formatMoney(details[key])
                  : key === 'focusUnitCount'
                    ? formatNumber(details[key])
                    : details[key]
            }
          />
        ))}
      </dl>

      <p className={`${ui.typography.eyebrow} mt-5`}>{t('details.scope')}</p>
      <p className={ui.typography.hint}>{scopeText}</p>

      <button
        type="button"
        className={`${ui.controls.primaryButton} mt-3 w-full`}
        onClick={() => onExport(scope, exportFileBase(node))}
        disabled={exporting}
      >
        {exporting ? t('export.running') : t('export.button')}
      </button>

      {message ? (
        <p role="alert" className={`${ui.feedback.inlineError} mt-2`}>
          {message}
        </p>
      ) : null}
    </section>
  )
}

function Row({ label, value }) {
  return (
    <div className="flex items-baseline justify-between gap-4 py-2">
      <dt className="text-xs text-slate-500">{label}</dt>
      <dd className="text-right text-sm font-medium text-slate-800">{value}</dd>
    </div>
  )
}

function describeScope(node, i18n) {
  const { t, formatMonth } = i18n
  const parts = []
  if (node.year) parts.push(String(node.year))
  if (node.month) parts.push(formatMonth(node.month))
  if (node.categoryId) parts.push(`${t('node.category')} ${node.categoryId}`)
  if (node.productId) parts.push(`${t('node.product')} ${node.productId}`)
  if (node.orderId) parts.push(`${t('node.order')} ${node.orderId}`)
  return parts.length ? parts.join(' · ') : t('details.scopeAll')
}

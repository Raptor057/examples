/**
 * El servidor manda datos; el cliente redacta. Aqui se convierte el nodo (tipo + metricas) en
 * el texto que se ve, ya traducido y ya formateado por locale.
 */
export function nodeTitle(node, i18n) {
  const { t, formatMonth } = i18n
  switch (node.nodeType) {
    case 'month':
      // El mes viaja como NUMERO: el nombre lo pone Intl en el idioma activo.
      return formatMonth(node.label)
    case 'orderLineGroup':
      return t('node.orderLineGroup')
    case 'orderSummaryGroup':
      return t('node.orderSummaryGroup')
    case 'orderMetric':
      return t(`metric.${node.details?.metric ?? ''}`)
    default:
      return node.label
  }
}

export function nodeSubtitle(node, i18n) {
  const { tCount, formatMoney } = i18n
  const metrics = node.metrics ?? {}

  switch (node.nodeType) {
    case 'year':
    case 'month':
    case 'category':
    case 'product':
      // El conteo lo calculo el motor: no salio de contar hijos en memoria.
      return `${tCount('tree.orderCount', Number(metrics.orderCount ?? 0))} · ${formatMoney(metrics.totalAmount)}`
    case 'order':
      return `${tCount('tree.lineCount', Number(metrics.lineCount ?? 0))} · ${formatMoney(metrics.totalAmount)}`
    case 'orderLineGroup':
      return `${tCount('tree.lineCount', Number(metrics.lineCount ?? 0))} · ${formatMoney(metrics.totalAmount)}`
    case 'orderLine':
      return `${tCount('tree.unitCount', Number(metrics.unitCount ?? 0))} · ${formatMoney(metrics.totalAmount)}`
    case 'orderMetric':
      return node.details?.metric === 'linesTotal'
        ? formatMoney(metrics.totalAmount)
        : new Intl.NumberFormat(i18n.locale).format(Number(metrics.totalAmount ?? 0))
    default:
      return ''
  }
}

/** Las coordenadas del nodo son, tal cual, el alcance de la descarga. Cero parametros nuevos. */
export function exportScopeOf(node) {
  if (!node) return null
  return {
    year: node.year,
    month: node.month,
    categoryId: node.categoryId,
    productId: node.productId,
    orderId: node.orderId,
  }
}

export function exportFileBase(node) {
  if (!node) return 'pedidos'
  const parts = ['pedidos', node.nodeType]
  if (node.year) parts.push(String(node.year))
  if (node.month) parts.push(String(node.month).padStart(2, '0'))
  if (node.orderId) parts.push(`o${node.orderId}`)
  else if (node.productId) parts.push(`p${node.productId}`)
  else if (node.categoryId) parts.push(`c${node.categoryId}`)
  return parts.join('_')
}

import { http } from './clients'

/**
 * Servicio del feature. Un solo endpoint sirve los seis niveles: el "level" que se manda es
 * SIEMPRE el nextLevel que vino en el nodo, nunca uno que el cliente deduzca. Por eso insertar
 * o reordenar un nivel es un cambio de servidor y no obliga a desplegar las dos orillas juntas.
 */
export function getOrdersTreeChildren(params, { signal } = {}) {
  return http.get('/orders-tree/children', { params, signal })
}

/**
 * Exportacion acotada al nodo. Recibe las MISMAS coordenadas que sirven para expandirlo.
 */
export function getOrdersTreeExport(params, { signal } = {}) {
  return http.get('/orders-tree/export', { params, signal })
}

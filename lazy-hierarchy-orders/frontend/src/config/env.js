// Resolucion de entorno en UN solo lugar. Ningun servicio arma URLs por su cuenta ni conoce
// puertos: en desarrollo el proxy de Vite reenvia /api al Host, y en produccion lo hace el
// servidor web que sirve el bundle.
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

// Tamanos de la exportacion. Paginas grandes porque el costo del OFFSET profundo crece con la
// profundidad, y paralelismo bajo porque subirlo convierte tu propia descarga en una negacion
// de servicio contra tu base. Cuatro es un punto razonable; medilo antes de tocarlo.
export const EXPORT_PAGE_SIZE = 20000
export const EXPORT_BATCH_SIZE = 4

// El unico nivel con fan-out grande pagina. Los demas vienen completos.
export const ORDERS_PAGE_SIZE = 50

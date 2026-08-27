// Resolucion de entorno en UN solo lugar. Ningun servicio arma URLs por su cuenta ni conoce
// puertos: en desarrollo el proxy de Vite reenvia /api al Host, y en produccion lo hace el
// servidor web que sirve el bundle.
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api'

// Tamano de pagina por defecto de las pantallas paginadas. El servidor tiene el suyo y un TOPE:
// este valor es una preferencia del cliente, no una garantia.
export const DEFAULT_PAGE_SIZE = 25

export const PAGE_SIZE_OPTIONS = [10, 25, 50, 100]

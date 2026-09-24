// Vacio a proposito: el front pega a rutas relativas y el proxy de Vite -o el servidor web en
// produccion- decide a donde van. Hardcodear una IP aqui es lo que obliga a recompilar el bundle
// para cambiar de entorno.
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? ''

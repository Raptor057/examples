import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react'
import { fetchDirectory, signIn } from '../../auth/session'
import { loadMyAccess } from '../../api/accessService'

const SessionContext = createContext(null)

/**
 * El estado de sesion de la consola: quien eres, que puedes, y si el enforcement bloquea.
 *
 * Tres decisiones que se ven aqui:
 *
 * 1. LOS PERMISOS LOS DICE EL SERVIDOR, en /api/access/me, y se vuelven a pedir cada vez que
 *    cambia la persona. El cliente no los deduce del nombre del rol ni los guarda entre sesiones.
 *
 * 2. ESCONDER ES CORTESIA, NO SEGURIDAD. Lo que `can()` decide es que se DIBUJA. Quien llame la
 *    ruta a mano se topa igual con un 403 del servidor, que es el unico punto que cuenta.
 *
 * 3. EL ESTADO DEL FLAG VIAJA CON LOS PERMISOS. Sin el, la pantalla no puede distinguir "no
 *    tengo el permiso" de "el enforcement esta apagado y se ve todo": son la misma lista vacia.
 */
export function SessionProvider({ children }) {
  const [directory, setDirectory] = useState([])
  const [identity, setIdentity] = useState(null)
  const [access, setAccess] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)

  const enter = useCallback(async (tenantCode, username) => {
    setLoading(true)
    setError(null)
    try {
      const signedIn = await signIn(tenantCode, username)
      setIdentity(signedIn)

      // Se piden DESPUES del token y en cada cambio de persona: los permisos se calculan por
      // peticion a partir de las pertenencias, asi que reusar los anteriores mentiria.
      const { data } = await loadMyAccess()
      setAccess(data)
    } catch (cause) {
      setError(cause.message)
      setAccess(null)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    let active = true

    ;(async () => {
      try {
        const tenants = await fetchDirectory()
        if (!active) return
        setDirectory(tenants)

        const first = tenants[0]
        const firstUser = first?.users?.[0]
        if (first && firstUser) await enter(first.code, firstUser.username)
        else setLoading(false)
      } catch (cause) {
        if (!active) return
        setError(cause.message)
        setLoading(false)
      }
    })()

    return () => {
      active = false
    }
  }, [enter])

  const value = useMemo(() => {
    const permissions = access?.permissions ?? []
    const enforcementEnabled = access?.enforcementEnabled ?? true

    return {
      directory,
      identity,
      access,
      loading,
      error,
      enter,
      enforcementEnabled,
      permissions,

      // Con el enforcement apagado se ve TODO, que es el comportamiento previo a la migracion:
      // el punto del modo auditoria es que nadie deje de trabajar mientras se ajustan los roles.
      can: (code) => !enforcementEnabled || permissions.includes(code),

      // Si de verdad lo tiene, al margen del flag. Lo usa la interfaz para avisar que algo se ve
      // solo porque el enforcement esta apagado.
      reallyHas: (code) => permissions.includes(code),
    }
  }, [directory, identity, access, loading, error, enter])

  return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>
}

export function useSession() {
  const context = useContext(SessionContext)
  if (!context) throw new Error('useSession se usa dentro de SessionProvider.')
  return context
}

import { useEffect, useRef, useState } from 'react'
import { ui } from '../../styles/designSystem'
import { LOCALES, useI18n } from '../../i18n'
import { clearSession, ensureSession, fetchTenants } from '../../auth/session'
import useLazyOrdersTree from './hooks/useLazyOrdersTree'
import useOrdersTreeExport from './hooks/useOrdersTreeExport'
import OrdersTree from './components/OrdersTree'
import OrdersTreeToolbar from './components/OrdersTreeToolbar'
import NodeDetails from './components/NodeDetails'
import ExportProgress from './components/ExportProgress'

/**
 * La pagina COMPONE. Quien decide es el hook, quien habla con la red es el servicio, quien
 * pinta son los componentes y quien transforma son los utils.
 */
export default function OrdersTreePage() {
  const { t, locale, setLocale } = useI18n()

  // El traductor va por referencia y fuera de las dependencias de los efectos. Si entrara,
  // cambiar de idioma volveria a pedir el token, pondria la sesion en "no lista" y con eso
  // reiniciaria el arbol que el usuario ya tenia abierto.
  const tRef = useRef(t)
  tRef.current = t

  const [tenants, setTenants] = useState([])
  const [tenantCode, setTenantCode] = useState('')
  const [sessionReady, setSessionReady] = useState(false)
  const [sessionError, setSessionError] = useState('')
  const [search, setSearch] = useState({ searchType: 'all', searchValue: '' })

  // Tenants disponibles: es andamiaje del ejemplo, para poder comprobar en vivo que el arbol de
  // una empresa no muestra ni un renglon de la otra.
  useEffect(() => {
    let active = true
    fetchTenants()
      .then((list) => {
        if (!active) return
        setTenants(list)
        setTenantCode((current) => current || list[0]?.code || '')
      })
      .catch((error) => {
        if (active) setSessionError(error?.message || tRef.current('errors.session'))
      })
    return () => {
      active = false
    }
  }, [])

  // Al cambiar de empresa se pide un token nuevo. El tenant que manda es el que va FIRMADO
  // dentro de ese token, no el parametro que el cliente mande despues.
  useEffect(() => {
    if (!tenantCode) return undefined
    let active = true
    setSessionReady(false)
    setSessionError('')
    clearSession()

    ensureSession(tenantCode)
      .then(() => {
        if (active) setSessionReady(true)
      })
      .catch((error) => {
        if (active) setSessionError(error?.message || tRef.current('errors.session'))
      })

    return () => {
      active = false
    }
  }, [tenantCode])

  const tree = useLazyOrdersTree({
    tenantCode,
    searchType: search.searchType,
    searchValue: search.searchValue,
    ready: sessionReady,
  })

  const exportState = useOrdersTreeExport({
    searchType: search.searchType,
    searchValue: search.searchValue,
  })

  return (
    <main className={ui.layout.appSection}>
      <header className={ui.layout.header}>
        <div>
          <h1 className={ui.typography.pageTitle}>{t('app.title')}</h1>
          <p className={`${ui.typography.body} mt-1 max-w-2xl`}>{t('app.subtitle')}</p>
        </div>

        <div className="flex items-end gap-3">
          <div className="w-44">
            <label className={ui.controls.label} htmlFor="tenant-select">
              {t('app.tenant')}
            </label>
            <select
              id="tenant-select"
              className={ui.controls.select}
              value={tenantCode}
              onChange={(event) => setTenantCode(event.target.value)}
            >
              {tenants.map((tenant) => (
                <option key={tenant.code} value={tenant.code}>
                  {tenant.name}
                </option>
              ))}
            </select>
          </div>

          <div className="w-28">
            <label className={ui.controls.label} htmlFor="locale-select">
              {t('app.language')}
            </label>
            <select
              id="locale-select"
              className={ui.controls.select}
              value={locale}
              onChange={(event) => setLocale(event.target.value)}
            >
              {LOCALES.map((code) => (
                <option key={code} value={code}>
                  {code.toUpperCase()}
                </option>
              ))}
            </select>
          </div>
        </div>
      </header>

      {sessionError ? (
        <p role="alert" className={`${ui.feedback.errorBanner} mt-4`}>
          {sessionError}
        </p>
      ) : null}

      <div className="mt-4">
        <OrdersTreeToolbar
          searchType={search.searchType}
          searchValue={search.searchValue}
          onApply={setSearch}
          disabled={!sessionReady}
        />
      </div>

      <div className={ui.layout.split}>
        <section aria-labelledby="tree-title">
          <h2 id="tree-title" className="sr-only">
            {t('tree.title')}
          </h2>
          {sessionReady ? <OrdersTree tree={tree} /> : <p className={ui.typography.body}>{t('app.loadingSession')}</p>}
        </section>

        <div className={ui.layout.detailColumn}>
          <NodeDetails
            node={tree.selectedNode}
            onExport={exportState.run}
            exporting={exportState.exporting}
            message={exportState.message}
          />
        </div>
      </div>

      <ExportProgress progress={exportState.progress} onCancel={exportState.cancel} cancelling={exportState.cancelling} />
    </main>
  )
}

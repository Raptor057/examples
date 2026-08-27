import { useState } from 'react'
import { useI18n } from '../../i18n'
import { ui } from '../../styles/designSystem'
import Badge from '../../components/Badge'
import Banner from '../../components/Banner'
import Pagination from '../../components/Pagination'
import AuditToolbar from './components/AuditToolbar'
import { useAuditLog } from './hooks/useAuditLog'

/**
 * LA BITACORA, EN SUS DOS MITADES.
 *
 * "Acciones" es lo que si ocurrio. "Intentos rechazados" es lo que alguien quiso hacer y no
 * pudo. Las dos viven en la misma pantalla a proposito: quien investiga un incidente no sabe de
 * antemano en cual de las dos esta la respuesta, y obligarlo a adivinar entre dos menus es
 * pedirle que ya sepa lo que vino a averiguar.
 *
 * ESTA PANTALLA NO TIENE ACCIONES. Ni editar, ni borrar, ni marcar como visto. La bitacora es
 * append-only, y una que se puede tocar desde la interfaz deja de servir para lo unico que
 * sirve: explicar despues que paso.
 */
export default function AuditPage() {
  const { t } = useI18n()
  const [tab, setTab] = useState('actions')
  const { filters, page, loading, error, update, clear } = useAuditLog(tab)

  const isActions = tab === 'actions'

  // Cambiar de pestana limpia los filtros. Las dos mitades no comparten campos -una filtra por
  // codigo de accion y la otra por permiso- y arrastrarlos produciria una lista vacia sin motivo
  // visible.
  function switchTab(next) {
    if (next === tab) return
    clear()
    setTab(next)
  }

  return (
    <section>
      <header>
        <h2 className={ui.typography.sectionTitle}>{t('audit.title')}</h2>
        <p className={`${ui.typography.body} mt-1 max-w-3xl`}>{t('audit.subtitle')}</p>
      </header>

      <div className={`${ui.layout.nav} mt-4`} role="tablist" aria-label={t('audit.title')}>
        <button
          type="button"
          role="tab"
          aria-selected={isActions}
          className={`${ui.controls.tab} ${isActions ? ui.controls.tabActive : ''}`}
          onClick={() => switchTab('actions')}
        >
          {t('audit.tabActions')}
        </button>
        <button
          type="button"
          role="tab"
          aria-selected={!isActions}
          className={`${ui.controls.tab} ${!isActions ? ui.controls.tabActive : ''}`}
          onClick={() => switchTab('denials')}
        >
          {t('audit.tabDenials')}
        </button>
      </div>

      <p className={`${ui.typography.hint} mt-3`}>{t('audit.readOnlyHint')}</p>

      <div className="mt-4">
        <AuditToolbar tab={tab} filters={filters} loading={loading} onChange={update} onClear={clear} />
      </div>

      {error ? (
        <div className="mt-4">
          <Banner tone="error">{error}</Banner>
        </div>
      ) : null}

      <div className="mt-4">
        {isActions ? <ActionsTable page={page} loading={loading} /> : <DenialsTable page={page} loading={loading} />}
      </div>

      {page ? (
        <Pagination
          pageNumber={page.pageNumber}
          pageSize={page.pageSize}
          totalCount={page.totalCount}
          loading={loading}
          onPageChange={(pageNumber) => update({ pageNumber })}
          onPageSizeChange={(pageSize) => update({ pageSize })}
        />
      ) : null}
    </section>
  )
}

function ActionsTable({ page, loading }) {
  const { t, formatDateTime } = useI18n()

  if (!page) return <p className={ui.feedback.empty}>{loading ? t('common.loading') : t('common.empty')}</p>
  if (page.items.length === 0) return <p className={ui.feedback.empty}>{t('common.empty')}</p>

  return (
    <div className={ui.table.scroller}>
      <table className={ui.table.table}>
        <thead>
          <tr>
            <th className={ui.table.th}>{t('audit.when')}</th>
            <th className={ui.table.th}>{t('audit.actionCode')}</th>
            <th className={ui.table.th}>{t('audit.subject')}</th>
            <th className={ui.table.th}>{t('audit.reason')}</th>
            <th className={ui.table.th}>{t('audit.performedBy')}</th>
            <th className={ui.table.th}>{t('audit.authorizedBy')}</th>
            <th className={ui.table.th}>{t('audit.externalEffect')}</th>
          </tr>
        </thead>
        <tbody>
          {page.items.map((item) => (
            <tr key={item.publicId}>
              {/* La fecha llega en UTC y se pinta en la hora de quien mira. La conversion vive
                  en el modulo de i18n, no aqui: una sola frontera para las dos direcciones. */}
              <td className={`${ui.table.td} whitespace-nowrap`}>{formatDateTime(item.occurredAtUtc)}</td>
              <td className={ui.table.td}>
                <span className="font-mono text-xs">{item.actionCode}</span>
              </td>
              <td className={ui.table.td}>
                <div>{item.subjectLabel}</div>
                <div className={ui.typography.hint}>
                  <span className="font-mono">
                    {item.subjectType}:{item.subjectKey}
                  </span>
                </div>
              </td>
              {/* El motivo es texto que alguien escribio, y puede ser largo. Se lee completo:
                  truncarlo vacia de sentido la columna que explica por que se hizo. */}
              <td className={`${ui.table.td} max-w-sm break-words`}>{item.reason}</td>
              <td className={ui.table.td}>
                <div>{item.performedByDisplay}</div>
                <div className={ui.typography.hint}>{item.performedBy}</div>
              </td>
              <td className={ui.table.td}>{item.authorizedBy ?? '-'}</td>
              <td className={ui.table.td}>
                <ExternalEffect item={item} />
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

/**
 * El efecto que NO comparte transaccion con la accion.
 *
 * Es la columna mas importante de la tabla y la que nadie pide. La accion se guardo en nuestra
 * base; el efecto de afuera pudo no aplicarse. Sin este dato, "quedo registrado" se confunde
 * con "quedo hecho", y esa confusion solo se descubre cuando alguien cuadra contra el otro
 * sistema semanas despues.
 */
function ExternalEffect({ item }) {
  const { t } = useI18n()

  if (!item.externalEffectName) return <span className={ui.typography.hint}>{t('audit.externalNone')}</span>

  if (item.externalEffectApplied === true) {
    return (
      <Badge tone="ok">
        {item.externalEffectName} {t('audit.externalApplied')}
      </Badge>
    )
  }

  // Pendiente o fallido. En ambar y no en rojo: no es que el sistema este roto, es que hay algo
  // que todavia le debemos al otro lado.
  return (
    <div>
      <Badge tone="warn">
        {item.externalEffectName} {t('audit.externalPending')}
      </Badge>
      {item.externalEffectError ? (
        <p className={`${ui.feedback.inlineError} mt-1 max-w-xs break-words`}>{item.externalEffectError}</p>
      ) : null}
    </div>
  )
}

function DenialsTable({ page, loading }) {
  const { t, formatDateTime } = useI18n()

  if (!page) return <p className={ui.feedback.empty}>{loading ? t('common.loading') : t('common.empty')}</p>
  if (page.items.length === 0) return <p className={ui.feedback.empty}>{t('common.empty')}</p>

  return (
    <div className={ui.table.scroller}>
      <table className={ui.table.table}>
        <thead>
          <tr>
            <th className={ui.table.th}>{t('audit.when')}</th>
            <th className={ui.table.th}>{t('audit.permissionCode')}</th>
            <th className={ui.table.th}>{t('audit.route')}</th>
            <th className={ui.table.th}>{t('audit.attemptedBy')}</th>
            <th className={ui.table.th}>{t('audit.blocked')}</th>
          </tr>
        </thead>
        <tbody>
          {page.items.map((item) => (
            <tr key={item.publicId} className={item.blocked ? undefined : ui.table.rowMuted}>
              <td className={`${ui.table.td} whitespace-nowrap`}>{formatDateTime(item.occurredAtUtc)}</td>
              <td className={ui.table.td}>
                <span className="font-mono text-xs">{item.permissionCode}</span>
              </td>
              <td className={ui.table.td}>
                <span className="font-mono text-xs">
                  {item.httpMethod} {item.route}
                </span>
              </td>
              <td className={ui.table.td}>
                <div>{item.attemptedByDisplay}</div>
                <div className={ui.typography.hint}>{item.attemptedBy}</div>
              </td>
              {/* La distincion que hace util esta tabla durante una migracion: con enforcement
                  apagado el intento NO se bloqueo, solo se registro. Son las filas que dicen a
                  quien vas a romper el dia que lo enciendas. */}
              <td className={ui.table.td}>
                {item.blocked ? (
                  <Badge tone="danger">{t('audit.blocked')}</Badge>
                ) : (
                  <Badge tone="warn">{t('audit.wouldHaveBlocked')}</Badge>
                )}
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  )
}

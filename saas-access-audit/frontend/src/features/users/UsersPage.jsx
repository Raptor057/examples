import { useState } from 'react'
import { useI18n } from '../../i18n'
import { ui } from '../../styles/designSystem'
import Badge from '../../components/Badge'
import Banner from '../../components/Banner'
import Pagination from '../../components/Pagination'
import ConfirmWithReasonDialog from '../../components/ConfirmWithReasonDialog'
import { useSession } from '../shell/SessionProvider'
import { PERMISSIONS } from '../permissions'
import { deactivateUser } from '../../api/accessService'
import { useUsers } from './hooks/useUsers'
import UsersToolbar from './components/UsersToolbar'

export default function UsersPage() {
  const { t, formatDateTime } = useI18n()
  const session = useSession()
  const { query, page, loading, error, update, reload } = useUsers()

  const [target, setTarget] = useState(null)
  const [busy, setBusy] = useState(false)
  const [dialogError, setDialogError] = useState(null)
  const [outcome, setOutcome] = useState(null)

  // LA COLUMNA DE ACCIONES NO SE DIBUJA SI NO SE PUEDE ACTUAR.
  // Un boton deshabilitado sin explicacion es peor que ninguno: invita a intentarlo y no dice
  // que falta. La excepcion -un control visible pero deshabilitado que SI dice que permiso
  // falta- esta en la pantalla de roles, donde ver la matriz sin poder cambiarla es util.
  const canDeactivate = session.can(PERMISSIONS.usersDeactivate)

  async function confirm(reason) {
    setBusy(true)
    setDialogError(null)
    try {
      const { data, message } = await deactivateUser(target.publicId, reason)
      setTarget(null)

      // El EXITO PARCIAL se pinta como advertencia, no como exito: la desactivacion ocurrio pero
      // el efecto externo quedo pendiente, y decir "listo" en verde seria mentir.
      setOutcome({
        tone: data.externalEffectApplied ? 'success' : 'warning',
        text: data.externalEffectApplied
          ? t('users.deactivated', { username: data.username })
          : message,
      })

      reload()
    } catch (cause) {
      setDialogError(cause.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <section>
      <h2 className={ui.typography.pageTitle}>{t('users.title')}</h2>
      <p className={`${ui.typography.body} mt-1`}>{t('users.subtitle')}</p>

      <div className="mt-4">
        <UsersToolbar query={query} loading={loading} onChange={update} />
      </div>

      {outcome ? (
        <div className="mt-4">
          <Banner tone={outcome.tone} onDismiss={() => setOutcome(null)} dismissLabel={t('common.close')}>
            {outcome.text}
          </Banner>
        </div>
      ) : null}

      {error ? (
        <div className="mt-4">
          <Banner tone="error" onDismiss={reload} dismissLabel={t('common.retry')}>
            {error}
          </Banner>
        </div>
      ) : null}

      <div className={`${ui.table.scroller} mt-4`}>
        <table className={ui.table.table}>
          <caption className="sr-only">{t('users.title')}</caption>
          <thead>
            <tr>
              <th scope="col" className={ui.table.th}>{t('users.username')}</th>
              <th scope="col" className={ui.table.th}>{t('users.displayName')}</th>
              <th scope="col" className={ui.table.th}>{t('users.groups')}</th>
              <th scope="col" className={ui.table.th}>{t('users.roles')}</th>
              <th scope="col" className={ui.table.th}>{t('users.status')}</th>
              {canDeactivate ? (
                <th scope="col" className={ui.table.th}>{t('users.actions')}</th>
              ) : null}
            </tr>
          </thead>
          <tbody>
            {loading && !page ? (
              <tr>
                <td className={ui.feedback.empty} colSpan={canDeactivate ? 6 : 5}>
                  {t('common.loading')}
                </td>
              </tr>
            ) : (page?.items ?? []).length === 0 ? (
              <tr>
                <td className={ui.feedback.empty} colSpan={canDeactivate ? 6 : 5}>
                  {t('common.empty')}
                </td>
              </tr>
            ) : (
              page.items.map((user) => (
                <tr key={user.publicId} className={user.isActive ? '' : ui.table.rowMuted}>
                  <td className={`${ui.table.td} font-mono text-xs`}>{user.username}</td>
                  <td className={ui.table.td}>
                    {user.displayName}
                    <span className={`${ui.typography.hint} block`}>{user.email}</span>
                  </td>
                  <td className={ui.table.td}>{user.groups.join(', ') || '-'}</td>
                  <td className={ui.table.td}>{user.roles.join(', ') || '-'}</td>
                  <td className={ui.table.td}>
                    <Badge tone={user.isActive ? 'ok' : 'danger'}>
                      {user.isActive ? t('users.active') : t('users.inactive')}
                    </Badge>
                    {user.deactivatedAtUtc ? (
                      <span className={`${ui.typography.hint} mt-1 block`}>
                        {t('users.deactivatedAt', { date: formatDateTime(user.deactivatedAtUtc) })}
                      </span>
                    ) : null}
                  </td>
                  {canDeactivate ? (
                    <td className={ui.table.td}>
                      {/* Lo ya ejecutado se muestra en SOLO LECTURA. Un boton activo sobre algo
                          que ya se hizo invita a intentarlo otra vez, y el usuario no puede
                          saber que hizo el segundo clic. */}
                      {user.isActive ? (
                        <button
                          type="button"
                          className={ui.controls.linkButton}
                          onClick={() => {
                            setDialogError(null)
                            setTarget(user)
                          }}
                        >
                          {t('users.deactivate')}
                        </button>
                      ) : (
                        <span className={ui.typography.hint}>{t('users.alreadyDone')}</span>
                      )}
                    </td>
                  ) : null}
                </tr>
              ))
            )}
          </tbody>
        </table>
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

      <ConfirmWithReasonDialog
        open={target !== null}
        danger
        busy={busy}
        error={dialogError}
        title={t('users.confirmTitle', { username: target?.username ?? '' })}
        body={t('users.confirmBody', { username: target?.username ?? '' })}
        confirmLabel={t('users.confirmAction')}
        onConfirm={confirm}
        onCancel={() => setTarget(null)}
      />
    </section>
  )
}

import { useCallback, useEffect, useState } from 'react'
import { useI18n } from '../../i18n'
import { ui } from '../../styles/designSystem'
import Badge from '../../components/Badge'
import Banner from '../../components/Banner'
import ConfirmWithReasonDialog from '../../components/ConfirmWithReasonDialog'
import { useSession } from '../shell/SessionProvider'
import { PERMISSIONS } from '../permissions'
import { loadRoles, setRolePermission } from '../../api/accessService'

/**
 * LA MATRIZ DE ROLES Y PERMISOS.
 *
 * Las columnas son el CATALOGO, que el backend siembra desde codigo en cada arranque. Por eso
 * esta pantalla no tiene un "agregar permiso": un permiso que el codigo no declara no existe, y
 * ofrecer darlo de alta desde aqui seria ofrecer crear permisos fantasma.
 *
 * AQUI VIVE LA EXCEPCION DELIBERADA a "si no se puede, no se dibuja": sin access:role:manage las
 * casillas se ven pero estan deshabilitadas, y cada una DICE QUE PERMISO FALTA. Consultar la
 * matriz sin poder cambiarla es util -es justo lo que hace un auditor- y esconderla dejaria a esa
 * persona sin poder responder "quien puede hacer esto".
 */
export default function RolesPage() {
  const { t } = useI18n()
  const session = useSession()

  const [data, setData] = useState(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState(null)
  const [pending, setPending] = useState(null)
  const [busy, setBusy] = useState(false)
  const [dialogError, setDialogError] = useState(null)
  const [outcome, setOutcome] = useState(null)

  const canManage = session.can(PERMISSIONS.rolesManage)

  const refresh = useCallback(async (signal) => {
    setLoading(true)
    setError(null)
    try {
      const { data: payload } = await loadRoles({ signal })
      setData(payload)
    } catch (cause) {
      if (cause.name !== 'AbortError') setError(cause.message)
    } finally {
      setLoading(false)
    }
  }, [])

  useEffect(() => {
    const controller = new AbortController()
    refresh(controller.signal)
    return () => controller.abort()
  }, [refresh])

  async function confirm(reason) {
    setBusy(true)
    setDialogError(null)
    try {
      const { data: result } = await setRolePermission(
        pending.role.publicId,
        pending.permission.code,
        pending.granted,
        reason,
      )

      setOutcome(
        result.changed
          ? t('roles.changed', {
              permission: pending.permission.displayName,
              role: pending.role.name,
              state: t(pending.granted ? 'roles.stateGranted' : 'roles.stateRevoked'),
            })
          : t('roles.noChange'),
      )

      setPending(null)
      await refresh()
    } catch (cause) {
      setDialogError(cause.message)
    } finally {
      setBusy(false)
    }
  }

  return (
    <section>
      <h2 className={ui.typography.pageTitle}>{t('roles.title')}</h2>
      <p className={`${ui.typography.body} mt-1`}>{t('roles.subtitle')}</p>

      {!canManage ? (
        <div className="mt-4">
          <Banner tone="info">{t('roles.readOnlyNotice')}</Banner>
        </div>
      ) : null}

      {outcome ? (
        <div className="mt-4">
          <Banner tone="success" onDismiss={() => setOutcome(null)} dismissLabel={t('common.close')}>
            {outcome}
          </Banner>
        </div>
      ) : null}

      {error ? (
        <div className="mt-4">
          <Banner tone="error" onDismiss={() => refresh()} dismissLabel={t('common.retry')}>
            {error}
          </Banner>
        </div>
      ) : null}

      {loading && !data ? (
        <p className={ui.feedback.empty}>{t('common.loading')}</p>
      ) : data ? (
        <div className={`${ui.table.scroller} mt-4`}>
          <table className={ui.table.table}>
            <caption className="sr-only">{t('roles.title')}</caption>
            <thead>
              <tr>
                <th scope="col" className={ui.table.th}>{t('roles.role')}</th>
                <th scope="col" className={ui.table.th}>{t('roles.groups')}</th>
                {data.permissions.map((permission) => (
                  <th key={permission.code} scope="col" className={ui.table.th}>
                    <span className="block">{permission.displayName}</span>
                    <span className="block font-mono text-[10px] font-normal normal-case text-slate-400">
                      {permission.code}
                    </span>
                    {permission.isDestructive ? (
                      <Badge tone="warn">{t('roles.destructive')}</Badge>
                    ) : null}
                  </th>
                ))}
              </tr>
            </thead>
            <tbody>
              {data.roles.map((role) => (
                <tr key={role.publicId}>
                  <th scope="row" className={`${ui.table.td} font-semibold text-slate-900`}>
                    {role.name}
                    <span className={`${ui.typography.mono} block`}>{role.code}</span>
                  </th>
                  <td className={ui.table.td}>{role.groups.join(', ') || '-'}</td>
                  {data.permissions.map((permission) => {
                    const granted = role.permissions.includes(permission.code)
                    const label = granted
                      ? t('roles.revoke', { permission: permission.displayName, role: role.name })
                      : t('roles.grant', { permission: permission.displayName, role: role.name })

                    return (
                      <td key={permission.code} className={ui.table.td}>
                        <label className="flex flex-col gap-1">
                          <span className="flex items-center gap-2">
                            <input
                              type="checkbox"
                              className={ui.controls.checkbox}
                              checked={granted}
                              disabled={!canManage || busy}
                              aria-label={label}
                              onChange={() => {
                                setDialogError(null)
                                setPending({ role, permission, granted: !granted })
                              }}
                            />
                            <span className={ui.typography.hint}>
                              {granted ? t('roles.granted') : t('roles.notGranted')}
                            </span>
                          </span>
                          {!canManage ? (
                            // El control deshabilitado DICE QUE FALTA. Un control apagado y mudo
                            // deja a la persona adivinando si es un permiso, un error o un bug.
                            <span className={ui.typography.hint}>
                              {t('common.missingPermission', { code: PERMISSIONS.rolesManage })}
                            </span>
                          ) : null}
                        </label>
                      </td>
                    )
                  })}
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      ) : null}

      <ConfirmWithReasonDialog
        open={pending !== null}
        danger={pending?.granted === false}
        busy={busy}
        error={dialogError}
        title={t(pending?.granted ? 'roles.confirmGrantTitle' : 'roles.confirmRevokeTitle', {
          permission: pending?.permission.displayName ?? '',
          role: pending?.role.name ?? '',
        })}
        body={t(pending?.granted ? 'roles.confirmGrantBody' : 'roles.confirmRevokeBody', {
          permission: pending?.permission.displayName ?? '',
          role: pending?.role.name ?? '',
        })}
        confirmLabel={t(pending?.granted ? 'roles.confirmGrantAction' : 'roles.confirmRevokeAction')}
        onConfirm={confirm}
        onCancel={() => setPending(null)}
      />
    </section>
  )
}

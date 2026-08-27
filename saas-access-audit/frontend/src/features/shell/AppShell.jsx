import { useState } from 'react'
import { useI18n, LOCALES } from '../../i18n'
import { ui } from '../../styles/designSystem'
import Badge from '../../components/Badge'
import Banner from '../../components/Banner'
import { useSession } from './SessionProvider'
import UsersPage from '../users/UsersPage'
import RolesPage from '../roles/RolesPage'
import AuditPage from '../audit/AuditPage'
import { PERMISSIONS } from '../permissions'

/**
 * El mapa RUTA -> PERMISO. Es lo unico que decide que aparece en el menu.
 *
 * Lo que no esta aqui se DIBUJA SIEMPRE; lo que esta, solo si la persona puede. Y esto es
 * cortesia, no seguridad: cada endpoint valida por su cuenta y responde 403 aunque el boton
 * nunca se haya visto.
 */
const TABS = [
  { key: 'users', labelKey: 'nav.users', permission: PERMISSIONS.usersView, Component: UsersPage },
  { key: 'roles', labelKey: 'nav.roles', permission: PERMISSIONS.rolesView, Component: RolesPage },
  { key: 'audit', labelKey: 'nav.audit', permission: PERMISSIONS.auditLogView, Component: AuditPage },
]

export default function AppShell() {
  const { t, locale, setLocale } = useI18n()
  const session = useSession()
  const [activeKey, setActiveKey] = useState('users')

  const visibleTabs = TABS.filter((tab) => session.can(tab.permission))
  const active = visibleTabs.find((tab) => tab.key === activeKey) ?? visibleTabs[0]
  const ActiveComponent = active?.Component

  return (
    <div className={ui.layout.page}>
      <header className={ui.layout.header}>
        <div>
          <h1 className={ui.typography.pageTitle}>{t('app.title')}</h1>
          <p className={`${ui.typography.body} mt-1 max-w-3xl`}>{t('app.subtitle')}</p>
        </div>
        <SessionBar locale={locale} setLocale={setLocale} />
      </header>

      {session.error ? (
        <div className="mt-4">
          <Banner tone="error">{session.error}</Banner>
        </div>
      ) : null}

      {!session.enforcementEnabled ? (
        <div className="mt-4">
          <Banner tone="warning" title={t('enforcement.offTitle')}>
            {t('enforcement.offBody')}
          </Banner>
        </div>
      ) : null}

      <IdentityStrip />

      <nav className={`${ui.layout.nav} mt-6`} aria-label={t('app.title')}>
        {visibleTabs.map((tab) => (
          <button
            key={tab.key}
            type="button"
            className={`${ui.controls.tab} ${active?.key === tab.key ? ui.controls.tabActive : ''}`}
            aria-current={active?.key === tab.key ? 'page' : undefined}
            onClick={() => setActiveKey(tab.key)}
          >
            {t(tab.labelKey)}
          </button>
        ))}
      </nav>

      <main id="contenido" className={ui.layout.section}>
        {session.loading && !session.access ? (
          <p className={ui.feedback.empty}>{t('app.signingIn')}</p>
        ) : ActiveComponent ? (
          <ActiveComponent />
        ) : (
          <p className={ui.feedback.empty}>{t('app.noPermissions')}</p>
        )}
      </main>
    </div>
  )
}

/** Selector de empresa, de persona y de idioma. Andamiaje del ejemplo. */
function SessionBar({ locale, setLocale }) {
  const { t } = useI18n()
  const { directory, identity, enter, loading } = useSession()

  const tenant = directory.find((item) => item.code === identity?.tenantCode) ?? directory[0]

  return (
    <div className="flex flex-wrap items-end gap-3">
      <div className={ui.layout.field}>
        <label className={ui.controls.label} htmlFor="tenant">
          {t('app.company')}
        </label>
        <select
          id="tenant"
          className={ui.controls.select}
          value={tenant?.code ?? ''}
          disabled={loading}
          onChange={(event) => {
            const next = directory.find((item) => item.code === event.target.value)
            if (next?.users?.length) enter(next.code, next.users[0].username)
          }}
        >
          {directory.map((item) => (
            <option key={item.code} value={item.code}>
              {item.name}
            </option>
          ))}
        </select>
      </div>

      <div className={ui.layout.field}>
        <label className={ui.controls.label} htmlFor="identity">
          {t('app.identity')}
        </label>
        <select
          id="identity"
          className={ui.controls.select}
          value={identity?.username ?? ''}
          disabled={loading}
          onChange={(event) => enter(tenant.code, event.target.value)}
        >
          {(tenant?.users ?? []).map((user) => (
            <option key={user.username} value={user.username}>
              {user.displayName}
            </option>
          ))}
        </select>
      </div>

      <div className={ui.layout.field}>
        <label className={ui.controls.label} htmlFor="locale">
          {t('app.language')}
        </label>
        <select
          id="locale"
          className={ui.controls.select}
          value={locale}
          onChange={(event) => setLocale(event.target.value)}
        >
          {LOCALES.map((item) => (
            <option key={item} value={item}>
              {item.toUpperCase()}
            </option>
          ))}
        </select>
      </div>
    </div>
  )
}

/**
 * Quien eres, por donde llegas a tus roles y que puedes. Es la pantalla que convierte un 403 en
 * un diagnostico: pertenencia -> rol -> permiso, con los tres saltos a la vista.
 */
function IdentityStrip() {
  const { t } = useI18n()
  const { identity, access, enforcementEnabled } = useSession()

  if (!identity || !access) return null

  return (
    <section className="mt-4 flex flex-wrap items-start gap-x-8 gap-y-3 border-b border-slate-200 pb-4">
      <div>
        <p className={ui.typography.eyebrow}>{t('app.groups')}</p>
        <p className={ui.typography.body}>{access.groups.join(', ') || '-'}</p>
      </div>
      <div>
        <p className={ui.typography.eyebrow}>{t('app.roles')}</p>
        <p className={ui.typography.body}>{access.roles.join(', ') || '-'}</p>
      </div>
      <div className="min-w-[18rem] flex-1">
        <p className={ui.typography.eyebrow}>{t('app.permissions')}</p>
        <div className="mt-1 flex flex-wrap gap-1">
          {access.permissions.length === 0 ? (
            <span className={ui.typography.hint}>{t('app.noPermissions')}</span>
          ) : (
            access.permissions.map((code) => (
              <Badge key={code} tone="neutral">
                <span className="font-mono">{code}</span>
              </Badge>
            ))
          )}
        </div>
      </div>
      <div>
        <p className={ui.typography.eyebrow}>&nbsp;</p>
        <Badge tone={enforcementEnabled ? 'ok' : 'warn'}>
          {enforcementEnabled ? t('enforcement.on') : t('enforcement.off')}
        </Badge>
      </div>
    </section>
  )
}

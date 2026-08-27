import { I18nProvider, useI18n } from './i18n'
import { SessionProvider } from './features/shell/SessionProvider'
import AppShell from './features/shell/AppShell'

export default function App() {
  return (
    <I18nProvider>
      <SkipLink />
      <SessionProvider>
        <AppShell />
      </SessionProvider>
    </I18nProvider>
  )
}

// Primer elemento enfocable de la pagina: evita tabular la cabecera entera en cada visita.
// Tambien pasa por i18n, como cualquier otro texto visible.
function SkipLink() {
  const { t } = useI18n()
  return (
    <a className="skip-link" href="#contenido">
      {t('app.skipToContent')}
    </a>
  )
}

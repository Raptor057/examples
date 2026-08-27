import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import es from './es.json'
import en from './en.json'

// El primero de la lista es el idioma por defecto y el destino del fallback.
export const LOCALES = ['es', 'en']
const DICTIONARIES = { es, en }
const DEFAULT_LOCALE = LOCALES[0]

const I18nContext = createContext(null)

function interpolate(template, vars) {
  if (!vars) return template
  return template.replace(/\{\{(\w+)\}\}/g, (match, name) =>
    Object.hasOwn(vars, name) ? String(vars[name]) : match,
  )
}

export function I18nProvider({ children }) {
  const [locale, setLocale] = useState(DEFAULT_LOCALE)

  const value = useMemo(() => {
    const dictionary = DICTIONARIES[locale] ?? DICTIONARIES[DEFAULT_LOCALE]
    const fallback = DICTIONARIES[DEFAULT_LOCALE]

    // Si falta la key en el idioma activo se cae al idioma por defecto; si tampoco esta, se
    // muestra la key cruda. Nunca un texto vacio y nunca un crash: la key a la vista es lo que
    // hace que el hueco se detecte en la primera pasada.
    const t = (key, vars) => interpolate(dictionary[key] ?? fallback[key] ?? key, vars)

    // Plural por sufijo _one / _other, resuelto por las reglas del locale y no por un if.
    const tCount = (key, count, vars) => {
      const rule = new Intl.PluralRules(locale).select(count)
      const template =
        dictionary[`${key}_${rule}`] ?? dictionary[`${key}_other`] ?? fallback[`${key}_other`] ?? key
      return interpolate(template, { count: formatNumber(locale, count), ...vars })
    }

    return {
      locale,
      setLocale,
      t,
      tCount,
      formatNumber: (value) => formatNumber(locale, value),
      formatDateTime: (value) => formatDateTime(locale, value),
    }
  }, [locale])

  // El titulo de la pestana tambien es texto visible: cambia con el idioma activo.
  useEffect(() => {
    document.title = value.t('app.title')
    document.documentElement.lang = locale
  }, [value, locale])

  return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>
}

export function useI18n() {
  const context = useContext(I18nContext)
  if (!context) throw new Error('useI18n se usa dentro de I18nProvider.')
  return context
}

function formatNumber(locale, value) {
  if (value === null || value === undefined) return ''
  return new Intl.NumberFormat(locale).format(Number(value))
}

// El backend serializa en UTC; el front es la UNICA capa que convierte a la hora local de quien
// mira. Por eso ninguna fecha llega ya formateada desde el servidor.
function formatDateTime(locale, value) {
  if (!value) return ''
  const raw = typeof value === 'string' && !value.endsWith('Z') ? `${value}Z` : value
  const date = new Date(raw)
  if (Number.isNaN(date.getTime())) return String(value)
  return new Intl.DateTimeFormat(locale, { dateStyle: 'short', timeStyle: 'medium' }).format(date)
}

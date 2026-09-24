import { useMemo } from 'react'

/**
 * El ZPL que se va a mandar, ya con los valores puestos, y un enlace para verlo dibujado en
 * Labelary. Es la forma mas barata de ver una etiqueta sin impresora, y evita el ciclo de
 * "imprime, mira el papel, corrige, vuelve a imprimir".
 *
 * OJO: esto es una APROXIMACION. El render de verdad lo hace el servidor, que ademas escapa los
 * caracteres de control de ZPL. Sirve para ver el acomodo, no para verificar el escapado.
 */
export default function ZplPreview({ template, values }) {
  const rendered = useMemo(() => {
    if (!template) return ''
    return template.body.replace(/\{\{\s*([A-Za-z0-9_]+)\s*\}\}/g, (_, name) => values[name.toUpperCase()] ?? '')
  }, [template, values])

  if (!template) return null

  const labelaryUrl =
    `http://api.labelary.com/v1/printers/${template.dpi === 300 ? '12' : '8'}dpmm/labels/4x6/0/` +
    encodeURIComponent(rendered)

  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-600">
          ZPL que se va a enviar
        </h2>
        <a
          href={labelaryUrl}
          target="_blank"
          rel="noreferrer"
          className="text-sm font-semibold text-sky-700 underline"
        >
          Ver la etiqueta dibujada
        </a>
      </div>
      <pre className="mt-3 max-h-64 overflow-auto rounded-md bg-slate-900 p-4 font-mono text-xs leading-relaxed text-slate-100">
        {rendered}
      </pre>
      <p className="mt-2 text-xs text-slate-500">
        Vista aproximada. El servidor ademas escapa los caracteres que ZPL interpreta como comandos.
      </p>
    </div>
  )
}

/**
 * El formulario se dibuja SOLO a partir de los marcadores que declara la plantilla. No hay una
 * lista de campos en el codigo del cliente: agregar {{LOTE}} al ZPL hace aparecer el campo, sin
 * tocar el front. Es lo que hace que las plantillas se puedan cambiar sin desplegar nada.
 */
export default function ValuesForm({ template, values, onChange }) {
  if (!template) return null

  if (template.placeholders.length === 0) {
    return (
      <p className="text-sm text-slate-500">
        Esta plantilla no tiene campos variables: se imprime tal cual.
      </p>
    )
  }

  return (
    <div>
      <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-600">
        Datos de la etiqueta
      </h2>
      <div className="mt-4 grid gap-4 sm:grid-cols-2">
        {template.placeholders.map((placeholder) => (
          <label key={placeholder} className="block">
            <span className="font-mono text-xs text-slate-600">{placeholder}</span>
            <input
              value={values[placeholder] ?? ''}
              onChange={(event) => onChange({ ...values, [placeholder]: event.target.value })}
              className="mt-1 block w-full min-h-11 rounded-md border border-slate-300 bg-white px-3 text-sm"
            />
          </label>
        ))}
      </div>
      <p className="mt-3 text-xs text-slate-500">
        Un campo vacio no impide imprimir: sale en blanco en la etiqueta y la API lo reporta.
      </p>
    </div>
  )
}

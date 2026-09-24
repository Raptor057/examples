export default function TemplatePicker({ dpi, onDpiChange, dpiOptions, templates, selectedCode, onSelect }) {
  return (
    <div className="space-y-4">
      <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-600">Plantilla</h2>

      <label className="block">
        <span className="text-sm text-slate-700">Resolucion</span>
        <select
          value={dpi}
          onChange={(event) => onDpiChange(Number(event.target.value))}
          className="mt-1 block w-full min-h-11 rounded-md border border-slate-300 bg-white px-3 text-sm"
        >
          {dpiOptions.map((option) => (
            <option key={option} value={option}>{option} dpi</option>
          ))}
        </select>
        {/* La razon de que esto exista, dicha donde se decide. */}
        <span className="mt-1 block text-xs text-slate-500">
          Cada resolucion tiene su propia version de la etiqueta: el ZPL va en puntos y no escala solo.
        </span>
      </label>

      <label className="block">
        <span className="text-sm text-slate-700">Etiqueta</span>
        <select
          value={selectedCode}
          onChange={(event) => onSelect(event.target.value)}
          disabled={templates.length === 0}
          className="mt-1 block w-full min-h-11 rounded-md border border-slate-300 bg-white px-3 text-sm disabled:bg-slate-100"
        >
          {templates.length === 0 ? <option value="">Sin plantillas</option> : null}
          {templates.map((template) => (
            <option key={template.code} value={template.code}>
              {template.code} - {template.name}
            </option>
          ))}
        </select>
      </label>
    </div>
  )
}

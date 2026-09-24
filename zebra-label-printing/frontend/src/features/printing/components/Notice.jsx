const STYLES = {
  success: 'border-emerald-300 bg-emerald-50 text-emerald-900',
  warning: 'border-amber-300 bg-amber-50 text-amber-900',
  error: 'border-red-300 bg-red-50 text-red-900',
  info: 'border-sky-300 bg-sky-50 text-sky-900',
}

export default function Notice({ notice, onClose }) {
  return (
    <div className={`flex items-start justify-between gap-4 rounded-md border px-4 py-3 text-sm ${STYLES[notice.type]}`}>
      <p>{notice.text}</p>
      <button type="button" onClick={onClose} className="shrink-0 font-semibold" aria-label="Cerrar aviso">
        x
      </button>
    </div>
  )
}

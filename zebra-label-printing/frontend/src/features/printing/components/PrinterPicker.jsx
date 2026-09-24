import { useState } from 'react'

export default function PrinterPicker({ printers, target, onChange, onRefresh }) {
  const [scanning, setScanning] = useState(false)

  async function scanNetwork() {
    setScanning(true)
    try {
      await onRefresh(true)
    } finally {
      setScanning(false)
    }
  }

  const installed = printers.filter((printer) => printer.transport === 'installed')

  return (
    <div className="space-y-4">
      <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-600">Impresora</h2>

      <div className="flex gap-4 text-sm">
        {['installed', 'network'].map((transport) => (
          <label key={transport} className="flex items-center gap-2">
            <input
              type="radio"
              name="transport"
              checked={target.transport === transport}
              onChange={() => onChange({ ...target, transport })}
            />
            {transport === 'installed' ? 'Instalada' : 'Por red'}
          </label>
        ))}
      </div>

      {target.transport === 'installed' ? (
        <label className="block">
          <span className="text-sm text-slate-700">Cola</span>
          <select
            value={target.queueName}
            onChange={(event) => onChange({ ...target, queueName: event.target.value })}
            className="mt-1 block w-full min-h-11 rounded-md border border-slate-300 bg-white px-3 text-sm"
          >
            <option value="">Elige una impresora</option>
            {installed.map((printer) => (
              <option key={printer.name} value={printer.name}>{printer.name}</option>
            ))}
          </select>
        </label>
      ) : (
        <div className="grid grid-cols-3 gap-3">
          <label className="col-span-2 block">
            <span className="text-sm text-slate-700">Direccion</span>
            <input
              value={target.address}
              onChange={(event) => onChange({ ...target, address: event.target.value })}
              placeholder="192.168.0.50"
              className="mt-1 block w-full min-h-11 rounded-md border border-slate-300 bg-white px-3 text-sm"
            />
          </label>
          <label className="block">
            <span className="text-sm text-slate-700">Puerto</span>
            <input
              type="number"
              value={target.port}
              onChange={(event) => onChange({ ...target, port: event.target.value })}
              className="mt-1 block w-full min-h-11 rounded-md border border-slate-300 bg-white px-3 text-sm"
            />
          </label>
        </div>
      )}

      <button
        type="button"
        onClick={scanNetwork}
        disabled={scanning}
        className="inline-flex min-h-11 items-center rounded-md border border-slate-300 bg-white px-4 text-sm font-semibold transition hover:bg-slate-100 disabled:opacity-50"
      >
        {scanning ? 'Buscando...' : 'Buscar impresoras en la red'}
      </button>
      {/* Se avisa ANTES de que el usuario crea que la pantalla se colgo. */}
      <p className="text-xs text-slate-500">Tarda unos segundos: manda un barrido a toda la subred.</p>
    </div>
  )
}

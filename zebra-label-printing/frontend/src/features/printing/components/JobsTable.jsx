/**
 * Lo ultimo que se mando, con su ZPL exacto. Es la pantalla que se abre cuando alguien dice
 * "esa etiqueta salio mal": la plantilla y los valores por separado no reconstruyen lo que de
 * verdad viajo al cabezal.
 */
export default function JobsTable({ jobs, onRefresh }) {
  return (
    <div>
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h2 className="text-xs font-semibold uppercase tracking-wide text-slate-600">
          Ultimas impresiones
        </h2>
        <button
          type="button"
          onClick={onRefresh}
          className="inline-flex min-h-11 items-center rounded-md border border-slate-300 bg-white px-4 text-sm font-semibold transition hover:bg-slate-100"
        >
          Refrescar
        </button>
      </div>

      {jobs.length === 0 ? (
        <p className="mt-4 text-sm text-slate-500">Todavia no se ha impreso nada.</p>
      ) : (
        <div className="mt-4 overflow-x-auto">
          <table className="w-full text-left text-sm">
            <thead className="border-b border-slate-200 text-xs uppercase tracking-wide text-slate-600">
              <tr>
                <th className="py-2 pr-4">Cuando</th>
                <th className="py-2 pr-4">Destino</th>
                <th className="py-2 pr-4">Plantilla</th>
                <th className="py-2 pr-4">Resultado</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-200">
              {jobs.map((job) => (
                <tr key={job.id}>
                  <td className="py-2 pr-4 tabular-nums">
                    {new Date(job.createdAtUtc).toLocaleString()}
                  </td>
                  <td className="py-2 pr-4">{job.target}</td>
                  <td className="py-2 pr-4">{job.templateCode ?? 'ZPL directo'}</td>
                  <td className="py-2 pr-4">
                    {job.succeeded ? (
                      <span className="text-emerald-700">Enviada</span>
                    ) : (
                      // El motivo del fallo se muestra completo: es el dato por el que se entro.
                      <span className="text-red-700" title={job.error ?? ''}>Fallo: {job.error}</span>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

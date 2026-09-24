import usePrintingConsole, { DPI_OPTIONS } from './features/printing/hooks/usePrintingConsole'
import DriverBanner from './features/printing/components/DriverBanner'
import Notice from './features/printing/components/Notice'
import PrinterPicker from './features/printing/components/PrinterPicker'
import TemplatePicker from './features/printing/components/TemplatePicker'
import ValuesForm from './features/printing/components/ValuesForm'
import ZplPreview from './features/printing/components/ZplPreview'
import JobsTable from './features/printing/components/JobsTable'

export default function App() {
  const console = usePrintingConsole()

  return (
    <div className="min-h-screen bg-slate-50 text-slate-900">
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto max-w-5xl px-4 py-5">
          <h1 className="text-xl font-semibold">Impresion de etiquetas</h1>
          <p className="mt-1 text-sm text-slate-600">
            Elige una plantilla, llena sus campos y mandala a una impresora.
          </p>
        </div>
      </header>

      <main className="mx-auto max-w-5xl space-y-8 px-4 py-6">
        <DriverBanner health={console.health} />

        {console.notice ? (
          <Notice notice={console.notice} onClose={() => console.setNotice(null)} />
        ) : null}

        {console.loading ? (
          <p className="py-10 text-center text-sm text-slate-500">Cargando...</p>
        ) : (
          <>
            {/* Se agrupa con espacio y lineas divisorias, no metiendo cada zona en su tarjeta:
                un formulario partido en cinco rectangulos se lee peor, no mejor. */}
            <section className="grid gap-8 border-t border-slate-200 pt-6 md:grid-cols-2">
              <TemplatePicker
                dpi={console.dpi}
                onDpiChange={console.setDpi}
                dpiOptions={DPI_OPTIONS}
                templates={console.templates}
                selectedCode={console.selectedCode}
                onSelect={console.setSelectedCode}
              />
              <PrinterPicker
                printers={console.printers}
                target={console.target}
                onChange={console.setTarget}
                onRefresh={console.refreshPrinters}
              />
            </section>

            <section className="border-t border-slate-200 pt-6">
              <ValuesForm
                template={console.selected}
                values={console.values}
                onChange={console.setValues}
              />

              <div className="mt-6 flex flex-wrap items-center gap-3">
                <button
                  type="button"
                  onClick={console.print}
                  disabled={!console.selected || console.printing}
                  className="inline-flex min-h-11 items-center rounded-md bg-slate-900 px-4 text-sm font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:opacity-50"
                >
                  {console.printing ? 'Enviando...' : 'Imprimir etiqueta'}
                </button>
                {!console.selected ? (
                  // Un boton gris sin explicacion se lee como pantalla rota.
                  <span className="text-sm text-slate-500">
                    No hay plantillas para {console.dpi} dpi.
                  </span>
                ) : null}
              </div>
            </section>

            <section className="border-t border-slate-200 pt-6">
              <ZplPreview template={console.selected} values={console.values} />
            </section>

            <section className="border-t border-slate-200 pt-6">
              <JobsTable jobs={console.jobs} onRefresh={console.refreshJobs} />
            </section>
          </>
        )}
      </main>
    </div>
  )
}

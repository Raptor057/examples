/**
 * Dice en grande que adaptador esta activo. Parece decoracion y no lo es: la pregunta numero uno
 * cuando alguien reporta "imprimi y no salio nada" es si estaba en simulador.
 */
export default function DriverBanner({ health }) {
  if (!health) return null

  const simulated = health.printerDriver === 'simulator'
  return (
    <div
      className={`rounded-md border px-4 py-3 text-sm ${
        simulated
          ? 'border-amber-300 bg-amber-50 text-amber-900'
          : 'border-emerald-300 bg-emerald-50 text-emerald-900'
      }`}
    >
      {simulated ? (
        <>
          <strong>Modo simulador.</strong> No se imprime nada: cada etiqueta se guarda como
          archivo <code>.zpl</code> en la carpeta del servidor. Para imprimir de verdad,
          pon <code>Printing:Driver</code> en <code>zebra</code>.
        </>
      ) : (
        <>
          <strong>Impresoras reales.</strong> Lo que mandes sale por el SDK de Link-OS.
        </>
      )}
    </div>
  )
}

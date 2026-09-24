import { useCallback, useEffect, useState } from 'react'
import {
  getHealth,
  listPrintJobs,
  listPrinters,
  listTemplates,
  printTemplate,
} from '../../../api/printingService'

/** Las resoluciones que acepta el servidor. Debe empatar con TemplateRules.SupportedDpi. */
export const DPI_OPTIONS = [203, 300, 600]

/**
 * Todo el estado de la pantalla en un hook. Los componentes quedan para pintar; aqui vive el
 * "que pasa cuando". Es lo que permite leer el flujo entero sin saltar entre cinco archivos.
 */
export default function usePrintingConsole() {
  const [health, setHealth] = useState(null)
  const [printers, setPrinters] = useState([])
  const [templates, setTemplates] = useState([])
  const [jobs, setJobs] = useState([])

  const [dpi, setDpi] = useState(203)
  const [selectedCode, setSelectedCode] = useState('')
  const [values, setValues] = useState({})
  const [target, setTarget] = useState({ transport: 'installed', queueName: '', address: '', port: 9100 })

  const [loading, setLoading] = useState(true)
  const [printing, setPrinting] = useState(false)
  const [notice, setNotice] = useState(null)

  const selected = templates.find((t) => t.code === selectedCode) ?? null

  const refreshJobs = useCallback(async () => {
    try {
      setJobs(await listPrintJobs(15))
    } catch (error) {
      setNotice({ type: 'error', text: error.message })
    }
  }, [])

  // Carga inicial. El AbortController evita que una respuesta tardia escriba estado despues de
  // que el componente se desmonto, que es el aviso de React que todo el mundo ignora hasta que
  // provoca un error raro.
  useEffect(() => {
    const controller = new AbortController()

    async function load() {
      setLoading(true)
      try {
        const [healthData, printerData, jobData] = await Promise.all([
          getHealth(controller.signal),
          listPrinters(false, controller.signal),
          listPrintJobs(15, controller.signal),
        ])
        setHealth(healthData)
        setPrinters(printerData)
        setJobs(jobData)
      } catch (error) {
        if (error.name !== 'AbortError') setNotice({ type: 'error', text: error.message })
      } finally {
        setLoading(false)
      }
    }

    load()
    return () => controller.abort()
  }, [])

  // Las plantillas se recargan al cambiar la resolucion: son LISTAS DISTINTAS, no la misma lista
  // filtrada. Un codigo puede existir para 203 y no para 300.
  useEffect(() => {
    const controller = new AbortController()

    listTemplates(dpi, controller.signal)
      .then((data) => {
        setTemplates(data)
        setSelectedCode((current) => (data.some((t) => t.code === current) ? current : data[0]?.code ?? ''))
      })
      .catch((error) => {
        if (error.name !== 'AbortError') setNotice({ type: 'error', text: error.message })
      })

    return () => controller.abort()
  }, [dpi])

  // Al cambiar de plantilla se conservan los valores cuyo marcador sigue existiendo. Borrarlos
  // todos obligaria a recapturar el numero de parte por cambiar de etiqueta.
  useEffect(() => {
    if (!selected) return
    setValues((current) => {
      const kept = {}
      for (const placeholder of selected.placeholders) {
        if (current[placeholder] !== undefined) kept[placeholder] = current[placeholder]
      }
      return kept
    })
  }, [selected])

  async function refreshPrinters(includeNetwork) {
    try {
      setPrinters(await listPrinters(includeNetwork))
      setNotice(includeNetwork ? { type: 'info', text: 'Barrido de red terminado.' } : null)
    } catch (error) {
      setNotice({ type: 'error', text: error.message })
    }
  }

  async function print() {
    if (!selected) return
    setPrinting(true)
    setNotice(null)
    try {
      const result = await printTemplate({ code: selected.code, dpi, target: buildTarget(target), values })

      // Un exito CON advertencia se dice distinto de un exito limpio: la etiqueta salio, pero
      // con huecos, y eso el operador tiene que saberlo antes de pegarla en una caja.
      setNotice(
        result.missingValues.length > 0
          ? { type: 'warning', text: `Etiqueta enviada, pero sin valor para: ${result.missingValues.join(', ')}.` }
          : { type: 'success', text: `Etiqueta enviada a ${result.receipt.target} (${result.receipt.bytesSent} bytes).` },
      )
      await refreshJobs()
    } catch (error) {
      setNotice({ type: 'error', text: error.message })
    } finally {
      setPrinting(false)
    }
  }

  return {
    health, printers, templates, jobs, selected,
    dpi, setDpi, selectedCode, setSelectedCode,
    values, setValues, target, setTarget,
    loading, printing, notice, setNotice,
    refreshPrinters, refreshJobs, print,
  }
}

/** Manda solo los campos del transporte elegido: el resto seria ruido para el servidor. */
function buildTarget(target) {
  return target.transport === 'network'
    ? { transport: 'network', address: target.address, port: Number(target.port) || 9100 }
    : { transport: 'installed', queueName: target.queueName }
}

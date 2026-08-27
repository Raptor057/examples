// Formato del archivo: comillas siempre, comillas internas duplicadas, CRLF entre renglones y
// BOM UTF-8 al inicio. Sin el BOM, Excel abre los acentos rotos.
const escapeCsv = (value) => `"${String(value ?? '').replace(/"/g, '""')}"`

/**
 * Convierte un lote de renglones a un BLOQUE de texto CSV, sin salto de linea final.
 *
 * Se llama UNA VEZ POR PAGINA. Esa es la diferencia entre soportar millones de filas y tumbar
 * la pestana: nunca se mantienen millones de arreglos vivos en memoria, porque cada pagina se
 * textualiza al vuelo y el arreglo original queda libre.
 */
export function rowsToCsvChunk(rows) {
  return rows.map((row) => row.map(escapeCsv).join(',')).join('\r\n')
}

/**
 * Arma el archivo desde los BLOQUES ya escapados y dispara la descarga.
 *
 * new Blob(partes) recibe un arreglo y lo une EL NAVEGADOR, sin materializar la concatenacion
 * en el heap de JavaScript. Es la linea que hace posible el export de millones de renglones;
 * acumular todo y hacer join('\n') al final revienta justo cuando el export importaba.
 */
export function downloadCsvChunks(chunks, fileBase = 'export') {
  const parts = ['﻿']
  chunks.forEach((chunk, index) => {
    parts.push(chunk)
    if (index < chunks.length - 1) parts.push('\r\n')
  })

  const blob = new Blob(parts, { type: 'text/csv;charset=utf-8;' })
  const url = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = url
  link.download = `${fileBase}_${fileTimestamp()}.csv`
  document.body.appendChild(link)
  link.click()
  document.body.removeChild(link)

  // Sin esto el blob se queda en memoria hasta que se cierre la pestana.
  URL.revokeObjectURL(url)
}

/** Fecha y hora en el nombre: sin eso el usuario acumula reporte(3).csv y no sabe cual es cual. */
function fileTimestamp() {
  const now = new Date()
  const pad = (value) => String(value).padStart(2, '0')
  return (
    `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}` +
    `-${pad(now.getHours())}${pad(now.getMinutes())}`
  )
}

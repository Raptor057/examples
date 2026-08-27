// Fuente unica de clases compartidas. Cero valores sueltos en los componentes: si un color o un
// espaciado aparece dos veces, vive aqui.
export const ui = {
  layout: {
    appSection: 'mx-auto w-full max-w-[110rem] px-4 py-6 sm:px-6 lg:px-8',
    header: 'flex flex-wrap items-start justify-between gap-4 border-b border-slate-200 pb-4',
    split: 'mt-6 grid grid-cols-1 gap-6 xl:grid-cols-[minmax(0,1fr)_24rem]',
    detailColumn: 'border-t border-slate-200 pt-6 xl:border-t-0 xl:border-l xl:pt-0 xl:pl-6',
    toolbar: 'flex flex-wrap items-end gap-3 border-b border-slate-200 pb-4',
  },
  typography: {
    pageTitle: 'text-xl font-black tracking-tight text-slate-900',
    sectionTitle: 'text-sm font-semibold text-slate-900',
    eyebrow: 'text-[11px] font-semibold uppercase tracking-wide text-slate-500',
    body: 'text-sm text-slate-600',
    hint: 'text-xs text-slate-500',
    mono: 'font-mono text-xs text-slate-500',
  },
  controls: {
    label: 'mb-1 block text-xs font-medium text-slate-600',
    input:
      'h-11 w-full rounded-lg border border-slate-300 px-3 text-sm text-slate-900 ' +
      'focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-900/20',
    select:
      'h-11 w-full rounded-lg border border-slate-300 bg-white px-3 text-sm text-slate-900 ' +
      'focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-900/20',
    primaryButton:
      'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-slate-900 px-4 text-sm ' +
      'font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50 ' +
      'focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/40',
    secondaryButton:
      'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg border border-slate-300 px-4 ' +
      'text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed ' +
      'disabled:opacity-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/40',
  },
  feedback: {
    errorBanner: 'rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-700',
    inlineError: 'text-xs text-red-600',
    empty: 'py-8 text-center text-sm text-slate-400',
  },
  tree: {
    row:
      'flex w-full cursor-pointer items-center gap-1 rounded-md py-1.5 pr-2 text-left transition-colors ' +
      'hover:bg-slate-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/40',
    rowSelected: 'bg-slate-900/5 ring-1 ring-slate-300',
    toggle:
      'flex size-6 shrink-0 items-center justify-center rounded text-slate-500 hover:bg-slate-200 ' +
      'focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/40',
    label: 'truncate text-sm font-medium text-slate-800',
    subLabel: 'shrink-0 text-xs text-slate-400',
  },
  overlay: {
    backdrop: 'fixed inset-0 z-40 flex items-center justify-center bg-slate-900/40 p-4',
    panel: 'w-full max-w-md rounded-xl border border-slate-200 bg-white p-5 shadow-lg',
    progressTrack: 'mt-3 h-2 w-full overflow-hidden rounded-full bg-slate-100',
    progressBar: 'h-full rounded-full bg-slate-900 transition-[width] duration-300',
  },
}

// Un tono por tipo de nodo, para que la profundidad se lea de un vistazo. Nunca es la UNICA
// senal: al lado siempre va el texto del tipo.
export const nodeTone = {
  year: 'bg-indigo-500',
  month: 'bg-violet-500',
  category: 'bg-teal-500',
  product: 'bg-cyan-500',
  order: 'bg-amber-500',
  orderLineGroup: 'bg-emerald-500',
  orderSummaryGroup: 'bg-slate-500',
  orderLine: 'bg-emerald-400',
  orderMetric: 'bg-slate-300',
}

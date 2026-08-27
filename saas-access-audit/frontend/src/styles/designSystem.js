// Fuente unica de clases compartidas. Cero valores sueltos en los componentes: si un color o un
// espaciado aparece dos veces, vive aqui.
//
// El diseno agrupa con ESPACIO, LINEAS y TIPOGRAFIA, no anidando tarjetas dentro de tarjetas.
// Una pantalla de administracion es una tabla y una barra de filtros; envolver cada cosa en su
// caja gris solo agrega ruido y hace que nada destaque.
export const ui = {
  layout: {
    page: 'mx-auto w-full max-w-[100rem] px-4 py-6 sm:px-6 lg:px-8',
    header: 'flex flex-wrap items-start justify-between gap-4 border-b border-slate-200 pb-4',
    section: 'mt-6',
    toolbar: 'flex flex-wrap items-end gap-3 border-b border-slate-200 pb-4',
    field: 'flex min-w-[10rem] flex-col',
    nav: 'flex flex-wrap gap-1 border-b border-slate-200',
    footerBar: 'flex flex-wrap items-center justify-between gap-3 border-t border-slate-200 pt-3',
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
    checkboxRow: 'flex items-center gap-2 text-sm text-slate-700',
    checkbox: 'size-4 rounded border-slate-300 text-slate-900 focus:ring-2 focus:ring-slate-900/30',
    textarea:
      'w-full rounded-lg border border-slate-300 p-3 text-sm text-slate-900 ' +
      'focus:border-slate-400 focus:outline-none focus:ring-2 focus:ring-slate-900/20',
    primaryButton:
      'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-slate-900 px-4 text-sm ' +
      'font-medium text-white transition hover:bg-slate-800 disabled:cursor-not-allowed disabled:opacity-50 ' +
      'focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/40',
    dangerButton:
      'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg bg-red-700 px-4 text-sm ' +
      'font-medium text-white transition hover:bg-red-800 disabled:cursor-not-allowed disabled:opacity-50 ' +
      'focus:outline-none focus-visible:ring-2 focus-visible:ring-red-700/40',
    secondaryButton:
      'inline-flex min-h-11 items-center justify-center gap-2 rounded-lg border border-slate-300 px-4 ' +
      'text-sm font-medium text-slate-700 transition hover:bg-slate-50 disabled:cursor-not-allowed ' +
      'disabled:opacity-50 focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/40',
    linkButton:
      'inline-flex items-center gap-1 rounded text-sm font-medium text-slate-700 underline ' +
      'underline-offset-2 hover:text-slate-900 disabled:cursor-not-allowed disabled:text-slate-400 ' +
      'disabled:no-underline focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/40',
    tab:
      'min-h-11 rounded-t-lg px-4 text-sm font-medium text-slate-600 transition hover:bg-slate-50 ' +
      'focus:outline-none focus-visible:ring-2 focus-visible:ring-slate-900/40',
    tabActive: 'border-b-2 border-slate-900 text-slate-900',
  },
  table: {
    scroller: 'w-full overflow-x-auto',
    table: 'w-full min-w-[52rem] border-collapse text-left text-sm',
    th: 'border-b border-slate-200 px-3 py-2 text-xs font-semibold uppercase tracking-wide text-slate-500',
    td: 'border-b border-slate-100 px-3 py-2 align-top text-slate-700',
    rowMuted: 'bg-slate-50/60 text-slate-400',
  },
  feedback: {
    errorBanner: 'rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-sm text-red-800',
    warningBanner: 'rounded-lg border border-amber-300 bg-amber-50 px-3 py-2 text-sm text-amber-900',
    infoBanner: 'rounded-lg border border-sky-200 bg-sky-50 px-3 py-2 text-sm text-sky-900',
    successBanner: 'rounded-lg border border-emerald-200 bg-emerald-50 px-3 py-2 text-sm text-emerald-900',
    empty: 'py-10 text-center text-sm text-slate-400',
    inlineError: 'text-xs text-red-700',
  },
  badge: {
    base: 'inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-[11px] font-medium',
    neutral: 'bg-slate-100 text-slate-700',
    ok: 'bg-emerald-100 text-emerald-800',
    warn: 'bg-amber-100 text-amber-900',
    danger: 'bg-red-100 text-red-800',
    info: 'bg-sky-100 text-sky-800',
  },
  overlay: {
    backdrop: 'fixed inset-0 z-40 flex items-center justify-center bg-slate-900/40 p-4',
    panel: 'w-full max-w-lg rounded-xl border border-slate-200 bg-white p-5 shadow-lg',
  },
}

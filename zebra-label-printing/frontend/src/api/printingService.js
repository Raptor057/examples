import { api } from './clients'

// Un servicio por dominio, con una funcion por endpoint. Los componentes no arman rutas ni
// cuerpos: piden lo que necesitan y el servicio sabe como se llama en la API.

export const getHealth = (signal) => api.get('/health', { signal })

export const listPrinters = (includeNetwork, signal) =>
  api.get('/api/printers', { params: { includeNetwork }, signal })

export const getPrinterStatus = (target, signal) =>
  api.post('/api/printers/status', target, { signal })

export const listTemplates = (dpi, signal) =>
  api.get('/api/templates', { params: { dpi }, signal })

export const saveTemplate = (template, isUpdate) =>
  isUpdate
    ? api.put(`/api/templates/${encodeURIComponent(template.code)}`, template)
    : api.post('/api/templates', template)

export const deactivateTemplate = (code, dpi) =>
  api.delete(`/api/templates/${encodeURIComponent(code)}`, { params: { dpi } })

export const printTemplate = (payload) => api.post('/api/print/template', payload)

export const sendRawZpl = (target, zpl) => api.post('/api/print/zpl', { target, zpl })

export const listPrintJobs = (take, signal) =>
  api.get('/api/print/jobs', { params: { take }, signal })

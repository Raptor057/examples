import { ui } from '../styles/designSystem'

/**
 * Etiqueta de estado. El color NUNCA es la unica senal: siempre lleva texto al lado, porque
 * rojo y verde son el mismo gris para bastante gente.
 */
export default function Badge({ tone = 'neutral', children, title }) {
  return (
    <span className={`${ui.badge.base} ${ui.badge[tone] ?? ui.badge.neutral}`} title={title}>
      {children}
    </span>
  )
}

import { context, propagation } from '@opentelemetry/api'
import type { H3Event } from 'h3'

export function getTraceHeaders(event: H3Event): Record<string, string> {
  const headers: Record<string, string> = {}
  const otelContext = event.context.otelContext ?? context.active()
  propagation.inject(otelContext, headers)
  return headers
}

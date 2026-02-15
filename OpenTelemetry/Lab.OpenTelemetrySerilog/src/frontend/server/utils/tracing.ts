import { context, propagation, trace, SpanKind, SpanStatusCode } from '@opentelemetry/api'
import type { H3Event } from 'h3'

export function getTraceHeaders(event: H3Event): Record<string, string> {
  const headers: Record<string, string> = {}
  const otelContext = event.context.otelContext ?? context.active()
  propagation.inject(otelContext, headers)
  return headers
}

/**
 * 包裹 $fetch 並自動建立 client span，將 trace context 注入到 outgoing headers
 */
export async function fetchWithTracing<T>(
  event: H3Event,
  url: string,
  options: Parameters<typeof $fetch>[1] & { baseURL?: string } = {},
): Promise<T> {
  const tracer = trace.getTracer('frontend')
  const parentContext = event.context.otelContext ?? context.active()
  const fullUrl = `${options.baseURL ?? ''}${url}`
  const method = (options.method as string)?.toUpperCase() ?? 'GET'

  return tracer.startActiveSpan(
    `${method} ${fullUrl}`,
    { kind: SpanKind.CLIENT },
    parentContext,
    async (span) => {
      // 從當前 span context 注入 trace headers
      const traceHeaders: Record<string, string> = {}
      propagation.inject(trace.setSpan(parentContext, span), traceHeaders)

      try {
        const response = await $fetch<T>(url, {
          ...options,
          headers: {
            ...options.headers,
            ...traceHeaders,
          },
        })
        span.setAttribute('http.response.status_code', 200)
        return response
      } catch (error: any) {
        span.setStatus({ code: SpanStatusCode.ERROR, message: error.message })
        if (error.statusCode) {
          span.setAttribute('http.response.status_code', error.statusCode)
        }
        throw error
      } finally {
        span.end()
      }
    },
  )
}

import { trace, context, propagation, SpanKind, SpanStatusCode } from '@opentelemetry/api'

export default defineEventHandler((event) => {
  const tracer = trace.getTracer('frontend')
  const url = getRequestURL(event)
  const method = getMethod(event)

  // 從 request headers 提取 incoming trace context
  const headers = getHeaders(event)
  const parentContext = propagation.extract(context.active(), headers)

  const span = tracer.startSpan(
    `${method} ${url.pathname}`,
    {
      kind: SpanKind.SERVER,
      attributes: {
        'http.request.method': method,
        'url.path': url.pathname,
        'url.full': url.toString(),
      },
    },
    parentContext,
  )

  // 將 OTel context 存入 event.context 供 API handler 使用
  event.context.otelContext = trace.setSpan(parentContext, span)

  event.node.res.on('finish', () => {
    span.setAttribute('http.response.status_code', event.node.res.statusCode)
    if (event.node.res.statusCode >= 400) {
      span.setStatus({ code: SpanStatusCode.ERROR })
    }
    span.end()
  })
})

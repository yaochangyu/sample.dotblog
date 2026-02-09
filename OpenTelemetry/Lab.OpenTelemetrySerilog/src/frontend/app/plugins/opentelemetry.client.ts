import { context, propagation } from '@opentelemetry/api'
import { WebTracerProvider } from '@opentelemetry/sdk-trace-web'
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { FetchInstrumentation } from '@opentelemetry/instrumentation-fetch'
import { registerInstrumentations } from '@opentelemetry/instrumentation'
import { resourceFromAttributes } from '@opentelemetry/resources'
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions'
import { ZoneContextManager } from '@opentelemetry/context-zone'
import { W3CTraceContextPropagator } from '@opentelemetry/core'

export default defineNuxtPlugin(() => {
  // Skip if running on server-side
  if (import.meta.server) {
    return
  }

  const config = useRuntimeConfig()
  const collectorUrl = config.public.otelCollectorUrl as string

  const resource = resourceFromAttributes({
    [ATTR_SERVICE_NAME]: 'frontend',
  })

  const exporter = new OTLPTraceExporter({
    url: `${collectorUrl}/v1/traces`,
  })

  const provider = new WebTracerProvider({
    resource,
    spanProcessors: [new BatchSpanProcessor(exporter)],
  })

  // Register the provider (v2.x API)
  provider.register()

  // Set up context manager
  const contextManager = new ZoneContextManager()
  contextManager.enable()
  context.setGlobalContextManager(contextManager)

  // Set up propagator
  propagation.setGlobalPropagator(new W3CTraceContextPropagator())

  registerInstrumentations({
    instrumentations: [
      new FetchInstrumentation({
        propagateTraceHeaderCorsUrls: [/\/api\//],
      }),
    ],
  })
})

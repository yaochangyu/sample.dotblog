import { NodeTracerProvider } from '@opentelemetry/sdk-trace-node'
import { BatchSpanProcessor } from '@opentelemetry/sdk-trace-base'
import { OTLPTraceExporter } from '@opentelemetry/exporter-trace-otlp-http'
import { resourceFromAttributes } from '@opentelemetry/resources'
import { ATTR_SERVICE_NAME } from '@opentelemetry/semantic-conventions'
import { W3CTraceContextPropagator } from '@opentelemetry/core'
import { propagation } from '@opentelemetry/api'

export default defineNitroPlugin(() => {
  const config = useRuntimeConfig()
  const exporterUrl = `${config.otelExporterUrl}/v1/traces`

  const provider = new NodeTracerProvider({
    resource: resourceFromAttributes({
      [ATTR_SERVICE_NAME]: 'frontend-server',
    }),
    spanProcessors: [
      new BatchSpanProcessor(
        new OTLPTraceExporter({ url: exporterUrl }),
      ),
    ],
  })

  provider.register()
  propagation.setGlobalPropagator(new W3CTraceContextPropagator())
})

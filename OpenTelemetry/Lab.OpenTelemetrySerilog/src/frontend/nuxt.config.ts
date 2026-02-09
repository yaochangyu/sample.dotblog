// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  runtimeConfig: {
    backendAUrl: 'http://localhost:5100',
    otelExporterUrl: process.env.OTEL_EXPORTER_URL || 'http://localhost:4318',
    public: {
      otelCollectorUrl: process.env.OTEL_COLLECTOR_URL || 'http://localhost:4318',
    },
  },

  nitro: {
    externals: {
      inline: [
        '@opentelemetry/api',
        '@opentelemetry/sdk-trace-base',
        '@opentelemetry/exporter-trace-otlp-http',
        '@opentelemetry/resources',
        '@opentelemetry/semantic-conventions',
        '@opentelemetry/core',
      ],
    },
  },
})

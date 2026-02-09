// https://nuxt.com/docs/api/configuration/nuxt-config
export default defineNuxtConfig({
  compatibilityDate: '2025-07-15',
  devtools: { enabled: true },

  runtimeConfig: {
    public: {
      otelCollectorUrl: process.env.OTEL_COLLECTOR_URL || 'http://localhost:4318',
    },
  },

  routeRules: {
    '/api/weather/**': {
      proxy: `${process.env.BACKEND_A_URL || 'http://localhost:5100'}/Weather/**`,
    },
    '/api/weather': {
      proxy: `${process.env.BACKEND_A_URL || 'http://localhost:5100'}/Weather`,
    },
  },
})

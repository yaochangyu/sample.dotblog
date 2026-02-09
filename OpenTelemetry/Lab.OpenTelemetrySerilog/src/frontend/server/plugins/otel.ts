// Server-side OpenTelemetry plugin
// Note: Currently disabled as frontend server primarily acts as a proxy
// and trace propagation is handled via HTTP headers

export default defineNitroPlugin(() => {
  // Plugin disabled - trace propagation handled via HTTP headers in API routes
})

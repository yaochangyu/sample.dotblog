export default defineEventHandler(async (event) => {
  const { backendAUrl } = useRuntimeConfig()
  const response = await $fetch('/Weather', {
    baseURL: backendAUrl,
    headers: {
      ...getProxyRequestHeaders(event),
      ...getTraceHeaders(event),
    },
  })
  return response
})

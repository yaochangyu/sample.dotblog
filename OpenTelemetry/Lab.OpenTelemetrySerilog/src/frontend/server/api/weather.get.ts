export default defineEventHandler(async (event) => {
  const { backendAUrl } = useRuntimeConfig()
  return await fetchWithTracing(event, '/Weather', {
    baseURL: backendAUrl,
    headers: {
      ...getProxyRequestHeaders(event),
    },
  })
})

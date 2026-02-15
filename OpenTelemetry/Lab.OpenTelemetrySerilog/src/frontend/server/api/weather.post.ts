export default defineEventHandler(async (event) => {
  const { backendAUrl } = useRuntimeConfig()
  const body = await readBody(event)
  return await fetchWithTracing(event, '/Weather', {
    baseURL: backendAUrl,
    method: 'POST',
    body,
    headers: {
      ...Object.fromEntries(
        Object.entries(getProxyRequestHeaders(event))
          .filter(([key]) => key.toLowerCase() !== 'content-length'),
      ),
    },
  })
})

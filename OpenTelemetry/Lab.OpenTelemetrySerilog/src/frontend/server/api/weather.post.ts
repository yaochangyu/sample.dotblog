export default defineEventHandler(async (event) => {
  const { backendAUrl } = useRuntimeConfig()
  const body = await readBody(event)
  const response = await $fetch('/Weather', {
    baseURL: backendAUrl,
    method: 'POST',
    body,
    headers: Object.fromEntries(
      Object.entries(getProxyRequestHeaders(event))
        .filter(([key]) => key.toLowerCase() !== 'content-length'),
    ),
  })
  return response
})

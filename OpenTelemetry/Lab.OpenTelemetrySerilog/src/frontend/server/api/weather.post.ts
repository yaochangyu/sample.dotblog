export default defineEventHandler(async (event) => {
  const { backendAUrl } = useRuntimeConfig()
  const body = await readBody(event)
  const response = await $fetch('/Weather', {
    baseURL: backendAUrl,
    method: 'POST',
    body,
    headers: getProxyRequestHeaders(event),
  })
  return response
})

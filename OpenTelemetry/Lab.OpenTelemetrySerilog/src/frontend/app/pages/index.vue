<script setup lang="ts">
interface WeatherForecast {
  date: string
  temperatureC: number
  temperatureF: number
  summary: string | null
}

const forecasts = ref<WeatherForecast[]>([])
const loading = ref(false)
const error = ref('')

const form = reactive({
  date: new Date().toISOString().split('T')[0],
  temperatureC: 25,
  summary: '',
})
const submitting = ref(false)

async function fetchWeather() {
  loading.value = true
  error.value = ''
  try {
    const data = await $fetch<WeatherForecast[]>('/api/weather')
    forecasts.value = data
  } catch (e: any) {
    error.value = e.message || '查詢失敗'
  } finally {
    loading.value = false
  }
}

async function addWeather() {
  submitting.value = true
  error.value = ''
  try {
    await $fetch('/api/weather', {
      method: 'POST',
      body: {
        date: form.date,
        temperatureC: form.temperatureC,
        summary: form.summary || null,
      },
    })
    await fetchWeather()
  } catch (e: any) {
    error.value = e.message || '新增失敗'
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <div style="max-width: 800px; margin: 0 auto; padding: 2rem; font-family: sans-serif;">
    <h1>Weather Forecast</h1>

    <!-- 查詢天氣 -->
    <section>
      <button :disabled="loading" @click="fetchWeather">
        {{ loading ? '查詢中...' : '查詢天氣' }}
      </button>

      <p v-if="error" style="color: red;">{{ error }}</p>

      <table v-if="forecasts.length" style="width: 100%; border-collapse: collapse; margin-top: 1rem;">
        <thead>
          <tr>
            <th style="border: 1px solid #ccc; padding: 0.5rem; text-align: left;">Date</th>
            <th style="border: 1px solid #ccc; padding: 0.5rem; text-align: right;">Temp (°C)</th>
            <th style="border: 1px solid #ccc; padding: 0.5rem; text-align: right;">Temp (°F)</th>
            <th style="border: 1px solid #ccc; padding: 0.5rem; text-align: left;">Summary</th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="f in forecasts" :key="f.date">
            <td style="border: 1px solid #ccc; padding: 0.5rem;">{{ f.date }}</td>
            <td style="border: 1px solid #ccc; padding: 0.5rem; text-align: right;">{{ f.temperatureC }}</td>
            <td style="border: 1px solid #ccc; padding: 0.5rem; text-align: right;">{{ f.temperatureF }}</td>
            <td style="border: 1px solid #ccc; padding: 0.5rem;">{{ f.summary }}</td>
          </tr>
        </tbody>
      </table>
    </section>

    <!-- 新增天氣 -->
    <section style="margin-top: 2rem;">
      <h2>新增天氣</h2>
      <form @submit.prevent="addWeather" style="display: flex; flex-direction: column; gap: 0.75rem; max-width: 400px;">
        <label>
          Date
          <input v-model="form.date" type="date" required style="display: block; width: 100%; padding: 0.25rem;">
        </label>
        <label>
          Temperature (°C)
          <input v-model.number="form.temperatureC" type="number" required style="display: block; width: 100%; padding: 0.25rem;">
        </label>
        <label>
          Summary
          <input v-model="form.summary" type="text" style="display: block; width: 100%; padding: 0.25rem;">
        </label>
        <button type="submit" :disabled="submitting">
          {{ submitting ? '提交中...' : '新增' }}
        </button>
      </form>
    </section>
  </div>
</template>

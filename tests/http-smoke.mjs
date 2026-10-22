const base = process.env.WORKFLOW_API_URL ?? 'http://127.0.0.1:52771'
const suffix = Math.random().toString(36).slice(2, 9)
let assertions = 0
function check(value, detail) { if (!value) throw new Error(detail); assertions++ }
async function request(path, method = 'GET', body, expected = 200) {
  const options = { method }
  if (body !== undefined) {
    if (body instanceof FormData) options.body = body
    else { options.headers = { 'Content-Type': 'application/json' }; options.body = JSON.stringify(body) }
  }
  const response = await fetch(base + path, options)
  const data = response.status === 204 ? null : await response.json().catch(() => null)
  check(response.status === expected, `${method} ${path}: expected ${expected}, got ${response.status} ${JSON.stringify(data)}`)
  return data
}
const category = await request('/api/categories', 'POST', { name: `   база   данных   ${suffix}   ` }, 201)
check(category.name === `База данных ${suffix}`, 'category normalization')
await request('/api/categories', 'POST', { name: `бАза данных ${suffix}` }, 409)
const second = await request('/api/categories', 'POST', { name: `Второе ${suffix}` }, 201)
const found = await request(`/api/categories?name=${encodeURIComponent(`база   данных ${suffix}`)}`)
check(found.some(x => x.id === category.id), 'category search')
await request('/api/chains', 'POST', { name: `Цепочка ${suffix}`, categoryIds: [] }, 400)
const chain = await request('/api/chains', 'POST', { name: `  цепочка  ${suffix}  `, categoryIds: [category.id, second.id] }, 201)
check(chain.name === `Цепочка ${suffix}` && chain.steps.length === 0, 'empty chain and normalized name')
await request('/api/chains', 'POST', { name: `ЦЕПОЧКА ${suffix}`, categoryIds: [category.id] }, 409)
await request(`/api/categories/${category.id}`, 'DELETE', undefined, 409)
const filtered = await request(`/api/chains?categoryId=${category.id}&name=${suffix}`)
check(filtered.length === 1 && filtered[0].id === chain.id, 'chain category and name filtering')
const one = await request(`/api/chains/${chain.id}/steps`, 'POST', { title: '', description: '' }, 201)
const two = await request(`/api/chains/${chain.id}/steps`, 'POST', { title: 'Конфигурация', description: '```cs\nvar x = 1;\n```' }, 201)
check(one.displayTitle === 'Шаг 1' && two.number === 2, 'automatic step numbering')
await request(`/api/chains/${chain.id}/steps/${two.id}/position`, 'PUT', { position: 1 })
let details = await request(`/api/chains/${chain.id}`)
check(details.steps[0].id === two.id && details.steps[1].displayTitle === 'Шаг 2', 'reordering and fallback title')
const external = 'http://127.0.0.1:9/image.png'
const failedImage = await request(`/api/chains/${chain.id}/steps/${two.id}`, 'PUT', { title: null, description: `![Внешнее](${external})` })
check(failedImage.description.includes(external) && failedImage.imageWarnings.length === 1, 'failed external image keeps source and returns warning')
const fencedImage = await request(`/api/chains/${chain.id}/steps/${two.id}`, 'PUT', { title: null, description: `\`\`\`md\n![Пример](${external})\n\`\`\`` })
check(fencedImage.description.includes(external) && fencedImage.imageWarnings.length === 0, 'code example does not download image')
const form = new FormData()
form.append('image', new Blob([Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jRZkAAAAASUVORK5CYII=', 'base64')], { type: 'image/png' }), 'screen.png')
const image = await request('/api/images', 'POST', form)
check(image.url.startsWith('/api/images/'), 'image upload URL')
const imageResponse = await fetch(base + image.url)
check(imageResponse.status === 200 && imageResponse.headers.get('content-type')?.startsWith('image/png'), 'image served')
await request(`/api/chains/${chain.id}/steps/${one.id}`, 'PUT', { title: null, description: `![Скриншот](${image.url})` })
await request(`/api/chains/${chain.id}/steps/${one.id}`, 'PUT', { title: null, description: '' })
const removedImage = await fetch(base + image.url)
check(removedImage.status === 404, 'unused image deleted')
await request(`/api/chains/${chain.id}/steps/${two.id}`, 'DELETE', undefined, 204)
details = await request(`/api/chains/${chain.id}`)
check(details.steps.length === 1 && details.steps[0].number === 1, 'step deletion renumbers')
await request(`/api/chains/${chain.id}`, 'PUT', { name: chain.name, categoryIds: [second.id] })
await request(`/api/categories/${category.id}`, 'DELETE', undefined, 204)
await request(`/api/chains/${chain.id}`, 'DELETE', undefined, 204)
await request(`/api/chains/${chain.id}/steps/${one.id}`, 'GET', undefined, 404)
await request(`/api/categories/${second.id}`, 'DELETE', undefined, 204)
console.log(`HTTP smoke passed: ${assertions} assertions`)

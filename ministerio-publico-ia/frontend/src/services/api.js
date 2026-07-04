import axios from 'axios'

const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5000/api'

export const api = axios.create({ baseURL: BASE_URL })

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('mp_ia_token')
  if (token) config.headers.Authorization = `Bearer ${token}`
  return config
})

api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('mp_ia_token')
      localStorage.removeItem('mp_ia_usuario')
      window.location.href = '/login'
    }
    return Promise.reject(error)
  }
)

// ---------------- Endpoints agrupados por dominio ----------------

export const authApi = {
  login: (nombreUsuario, contrasena) =>
    api.post('/Autenticacion/login', { nombreUsuario, contrasena }),
}

export const normalizacionApi = {
  cargaMasiva: (archivo, onProgress) => {
    const formData = new FormData()
    formData.append('archivo', archivo)
    return api.post('/Normalizacion/carga-masiva', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
      onUploadProgress: onProgress,
    })
  },
}

export const busquedaApi = {
  buscar: (textoDelito, cantidadResultados = 10) =>
    api.post('/Busqueda', { textoDelito, cantidadResultados }),
}

export const agrupamientoApi = {
  generarPropuestas: (umbralSimilitud = 80) =>
    api.post(`/Agrupamiento/generar-propuestas?umbralSimilitud=${umbralSimilitud}`),
  obtenerPendientes: () => api.get('/Agrupamiento/pendientes'),
}

export const validacionApi = {
  validar: (payload) => api.post('/ValidacionJuridica/validar', payload),
}

export const catalogoApi = {
  obtenerTodos: () => api.get('/CatalogoPenal'),
  obtenerPorId: (id) => api.get(`/CatalogoPenal/${id}`),
  crear: (payload) => api.post('/CatalogoPenal', payload),
}

export const dashboardApi = {
  obtenerIndicadores: () => api.get('/Dashboard/indicadores'),
}

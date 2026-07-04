import { createContext, useContext, useState, useCallback } from 'react'
import { authApi } from '../services/api'

const AuthContext = createContext(null)

export function AuthProvider({ children }) {
  const [usuario, setUsuario] = useState(() => {
    const guardado = localStorage.getItem('mp_ia_usuario')
    return guardado ? JSON.parse(guardado) : null
  })

  const iniciarSesion = useCallback(async (nombreUsuario, contrasena) => {
    const { data } = await authApi.login(nombreUsuario, contrasena)
    localStorage.setItem('mp_ia_token', data.token)
    localStorage.setItem('mp_ia_usuario', JSON.stringify({
      nombreCompleto: data.nombreCompleto,
      roles: data.roles,
    }))
    setUsuario({ nombreCompleto: data.nombreCompleto, roles: data.roles })
    return data
  }, [])

  const cerrarSesion = useCallback(() => {
    localStorage.removeItem('mp_ia_token')
    localStorage.removeItem('mp_ia_usuario')
    setUsuario(null)
  }, [])

  const tieneRol = useCallback((...roles) => {
    if (!usuario) return false
    return roles.some((r) => usuario.roles?.includes(r))
  }, [usuario])

  return (
    <AuthContext.Provider value={{ usuario, iniciarSesion, cerrarSesion, tieneRol }}>
      {children}
    </AuthContext.Provider>
  )
}

export function useAuth() {
  const ctx = useContext(AuthContext)
  if (!ctx) throw new Error('useAuth debe usarse dentro de AuthProvider')
  return ctx
}

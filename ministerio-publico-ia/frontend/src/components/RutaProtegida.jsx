import { Navigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext.jsx'

export function RutaProtegida({ children, rolesPermitidos }) {
  const { usuario, tieneRol } = useAuth()

  if (!usuario) return <Navigate to="/login" replace />

  if (rolesPermitidos && !tieneRol(...rolesPermitidos)) {
    return <Navigate to="/" replace />
  }

  return children
}

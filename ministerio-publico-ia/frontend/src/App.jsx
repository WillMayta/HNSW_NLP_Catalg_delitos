import { Routes, Route } from 'react-router-dom'
import { LayoutPrincipal } from './components/LayoutPrincipal.jsx'
import { RutaProtegida } from './components/RutaProtegida.jsx'
import { PaginaLogin } from './pages/PaginaLogin.jsx'
import { PaginaDashboard } from './pages/PaginaDashboard.jsx'
import { PaginaCargaMasiva } from './pages/PaginaCargaMasiva.jsx'
import { PaginaBusqueda } from './pages/PaginaBusqueda.jsx'
import { PaginaAgrupamiento } from './pages/PaginaAgrupamiento.jsx'
import { PaginaValidacionJuridica } from './pages/PaginaValidacionJuridica.jsx'
import { PaginaCatalogoPenal } from './pages/PaginaCatalogoPenal.jsx'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<PaginaLogin />} />

      <Route
        path="/"
        element={
          <RutaProtegida>
            <LayoutPrincipal />
          </RutaProtegida>
        }
      >
        <Route index element={<PaginaDashboard />} />
        <Route
          path="carga"
          element={
            <RutaProtegida rolesPermitidos={['Administrador', 'Analista']}>
              <PaginaCargaMasiva />
            </RutaProtegida>
          }
        />
        <Route path="busqueda" element={<PaginaBusqueda />} />
        <Route
          path="agrupamiento"
          element={
            <RutaProtegida rolesPermitidos={['Administrador', 'Analista']}>
              <PaginaAgrupamiento />
            </RutaProtegida>
          }
        />
        <Route
          path="validacion"
          element={
            <RutaProtegida rolesPermitidos={['EspecialistaJuridico', 'Administrador']}>
              <PaginaValidacionJuridica />
            </RutaProtegida>
          }
        />
        <Route path="catalogo" element={<PaginaCatalogoPenal />} />
      </Route>
    </Routes>
  )
}

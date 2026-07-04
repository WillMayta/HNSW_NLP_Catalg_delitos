import { useEffect, useState } from 'react'
import { Box, Grid, Paper, Typography, Skeleton, Alert } from '@mui/material'
import {
  LineChart, Line, BarChart, Bar, XAxis, YAxis, CartesianGrid, Tooltip,
  ResponsiveContainer, Cell,
} from 'recharts'
import DescriptionOutlinedIcon from '@mui/icons-material/DescriptionOutlined'
import AccountTreeOutlinedIcon from '@mui/icons-material/AccountTreeOutlined'
import VerifiedOutlinedIcon from '@mui/icons-material/VerifiedOutlined'
import PendingActionsOutlinedIcon from '@mui/icons-material/PendingActionsOutlined'
import { dashboardApi } from '../services/api.js'
import { tokens } from '../theme/theme.js'

function TarjetaIndicador({ titulo, valor, icono: Icono, acento, sufijo = '' }) {
  return (
    <Paper className="borde-folio" sx={{ p: 2.5, bgcolor: tokens.panel, height: '100%' }}>
      <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between' }}>
        <Box>
          <Typography sx={{ fontSize: '0.72rem', color: tokens.textoTenue, mb: 1, textTransform: 'uppercase', letterSpacing: '0.04em' }}>
            {titulo}
          </Typography>
          <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '1.9rem', fontWeight: 600, color: tokens.texto }}>
            {valor}{sufijo}
          </Typography>
        </Box>
        <Box sx={{
          width: 38, height: 38, borderRadius: '6px', bgcolor: `${acento}22`,
          display: 'flex', alignItems: 'center', justifyContent: 'center',
        }}>
          <Icono sx={{ fontSize: 19, color: acento }} />
        </Box>
      </Box>
    </Paper>
  )
}

export function PaginaDashboard() {
  const [datos, setDatos] = useState(null)
  const [error, setError] = useState('')
  const [cargando, setCargando] = useState(true)

  useEffect(() => {
    dashboardApi.obtenerIndicadores()
      .then(({ data }) => setDatos(data))
      .catch(() => setError('No se pudieron cargar los indicadores. Verifique la conexión con la API.'))
      .finally(() => setCargando(false))
  }, [])

  if (cargando) {
    return (
      <Box>
        <Skeleton variant="text" width={260} height={40} sx={{ mb: 3 }} />
        <Grid container spacing={2.5}>
          {[1, 2, 3, 4].map((i) => (
            <Grid item xs={12} sm={6} md={3} key={i}>
              <Skeleton variant="rounded" height={110} />
            </Grid>
          ))}
        </Grid>
      </Box>
    )
  }

  if (error) return <Alert severity="warning">{error}</Alert>

  return (
    <Box>
      <Box sx={{ mb: 4 }}>
        <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.68rem', color: tokens.sello, letterSpacing: '0.1em', mb: 0.5 }}>
          PANEL INSTITUCIONAL
        </Typography>
        <Typography variant="h4" sx={{ fontSize: '1.6rem' }}>Calidad del dato y consolidación</Typography>
      </Box>

      <Grid container spacing={2.5} sx={{ mb: 3 }}>
        <Grid item xs={12} sm={6} md={3}>
          <TarjetaIndicador titulo="Expedientes cargados" valor={datos.totalDelitosOriginales?.toLocaleString('es-PE')} icono={DescriptionOutlinedIcon} acento={tokens.info} />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <TarjetaIndicador titulo="Variantes detectadas" valor={datos.totalVariantesDetectadas?.toLocaleString('es-PE')} icono={AccountTreeOutlinedIcon} acento={tokens.sello} />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <TarjetaIndicador titulo="Delitos consolidados" valor={datos.totalDelitosCatalogoConsolidados?.toLocaleString('es-PE')} icono={VerifiedOutlinedIcon} acento={tokens.validado} />
        </Grid>
        <Grid item xs={12} sm={6} md={3}>
          <TarjetaIndicador titulo="Pendientes de validación" valor={datos.propuestasPendientesValidacion?.toLocaleString('es-PE')} icono={PendingActionsOutlinedIcon} acento={tokens.alerta} />
        </Grid>
      </Grid>

      <Grid container spacing={2.5} sx={{ mb: 3 }}>
        <Grid item xs={12} md={7}>
          <Paper sx={{ p: 3, bgcolor: tokens.panel, height: 340 }}>
            <Typography sx={{ fontSize: '0.85rem', fontWeight: 600, mb: 2 }}>Evolución temporal de casos</Typography>
            <ResponsiveContainer width="100%" height="88%">
              <LineChart data={datos.evolucionTemporal}>
                <CartesianGrid strokeDasharray="3 3" stroke={tokens.borde} />
                <XAxis dataKey="periodo" tick={{ fill: tokens.textoTenue, fontSize: 11 }} />
                <YAxis tick={{ fill: tokens.textoTenue, fontSize: 11 }} />
                <Tooltip contentStyle={{ background: tokens.panelAlto, border: `1px solid ${tokens.borde}`, borderRadius: 6 }} />
                <Line type="monotone" dataKey="cantidadCasos" name="Casos ingresados" stroke={tokens.info} strokeWidth={2} dot={false} />
                <Line type="monotone" dataKey="cantidadConsolidados" name="Consolidados" stroke={tokens.validado} strokeWidth={2} dot={false} />
              </LineChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>

        <Grid item xs={12} md={5}>
          <Paper sx={{ p: 3, bgcolor: tokens.panel, height: 340, display: 'flex', flexDirection: 'column' }}>
            <Typography sx={{ fontSize: '0.85rem', fontWeight: 600, mb: 1 }}>Porcentaje de registros consolidados</Typography>
            <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center' }}>
              <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '3.2rem', fontWeight: 700, color: tokens.sello }}>
                {datos.porcentajeRegistrosConsolidados}%
              </Typography>
              <Typography sx={{ fontSize: '0.78rem', color: tokens.textoTenue, textAlign: 'center', mt: 1 }}>
                de las denominaciones normalizadas ya cuentan con<br />validación jurídica en el catálogo penal
              </Typography>
            </Box>
          </Paper>
        </Grid>
      </Grid>

      <Grid container spacing={2.5}>
        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3, bgcolor: tokens.panel, height: 320 }}>
            <Typography sx={{ fontSize: '0.85rem', fontWeight: 600, mb: 2 }}>Delitos con mayor inconsistencia</Typography>
            <ResponsiveContainer width="100%" height="86%">
              <BarChart data={datos.delitosConMayorInconsistencia} layout="vertical" margin={{ left: 24 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={tokens.borde} horizontal={false} />
                <XAxis type="number" tick={{ fill: tokens.textoTenue, fontSize: 11 }} />
                <YAxis type="category" dataKey="nombreGenerico" tick={{ fill: tokens.textoTenue, fontSize: 10 }} width={140} />
                <Tooltip contentStyle={{ background: tokens.panelAlto, border: `1px solid ${tokens.borde}`, borderRadius: 6 }} />
                <Bar dataKey="cantidadVariantes" radius={[0, 4, 4, 0]}>
                  {datos.delitosConMayorInconsistencia?.map((_, i) => (
                    <Cell key={i} fill={tokens.alerta} fillOpacity={1 - i * 0.07} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>

        <Grid item xs={12} md={6}>
          <Paper sx={{ p: 3, bgcolor: tokens.panel, height: 320 }}>
            <Typography sx={{ fontSize: '0.85rem', fontWeight: 600, mb: 2 }}>Fiscalías con mayor variabilidad</Typography>
            <ResponsiveContainer width="100%" height="86%">
              <BarChart data={datos.fiscaliasConMayorVariabilidad} layout="vertical" margin={{ left: 24 }}>
                <CartesianGrid strokeDasharray="3 3" stroke={tokens.borde} horizontal={false} />
                <XAxis type="number" tick={{ fill: tokens.textoTenue, fontSize: 11 }} />
                <YAxis type="category" dataKey="siglaFiscalia" tick={{ fill: tokens.textoTenue, fontSize: 10 }} width={140} />
                <Tooltip contentStyle={{ background: tokens.panelAlto, border: `1px solid ${tokens.borde}`, borderRadius: 6 }} />
                <Bar dataKey="cantidadDenominacionesDistintas" radius={[0, 4, 4, 0]}>
                  {datos.fiscaliasConMayorVariabilidad?.map((_, i) => (
                    <Cell key={i} fill={tokens.info} fillOpacity={1 - i * 0.07} />
                  ))}
                </Bar>
              </BarChart>
            </ResponsiveContainer>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  )
}

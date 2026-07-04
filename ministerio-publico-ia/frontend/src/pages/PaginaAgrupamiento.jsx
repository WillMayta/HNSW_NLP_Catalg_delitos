import { useEffect, useState } from 'react'
import {
  Box, Paper, Typography, Button, Chip, Slider, Alert, LinearProgress,
} from '@mui/material'
import HubOutlinedIcon from '@mui/icons-material/HubOutlined'
import { agrupamientoApi } from '../services/api.js'
import { tokens } from '../theme/theme.js'

export function PaginaAgrupamiento() {
  const [umbral, setUmbral] = useState(80)
  const [propuestas, setPropuestas] = useState([])
  const [cargando, setCargando] = useState(false)
  const [generando, setGenerando] = useState(false)
  const [error, setError] = useState('')

  const cargarPendientes = async () => {
    setCargando(true)
    try {
      const { data } = await agrupamientoApi.obtenerPendientes()
      setPropuestas(data)
    } catch {
      setError('No se pudieron cargar las propuestas pendientes.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => { cargarPendientes() }, [])

  const handleGenerar = async () => {
    setGenerando(true)
    setError('')
    try {
      await agrupamientoApi.generarPropuestas(umbral)
      await cargarPendientes()
    } catch {
      setError('Ocurrió un error al ejecutar el motor de agrupamiento. Verifique que existan delitos normalizados pendientes.')
    } finally {
      setGenerando(false)
    }
  }

  return (
    <Box sx={{ maxWidth: 900 }}>
      <Box sx={{ mb: 4 }}>
        <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.68rem', color: tokens.sello, letterSpacing: '0.1em', mb: 0.5 }}>
          MOTOR INTELIGENTE
        </Typography>
        <Typography variant="h4" sx={{ fontSize: '1.6rem', mb: 1 }}>Propuestas de agrupación</Typography>
        <Typography sx={{ fontSize: '0.85rem', color: tokens.textoTenue }}>
          El motor agrupa automáticamente delitos similares, pero <strong>nunca asigna un delito
          genérico definitivo</strong>. Toda propuesta requiere validación de un especialista jurídico.
        </Typography>
      </Box>

      <Paper className="borde-folio" sx={{ p: 3, bgcolor: tokens.panel, mb: 3 }}>
        <Typography sx={{ fontSize: '0.8rem', fontWeight: 600, mb: 1 }}>
          Umbral de similitud mínima: {umbral}%
        </Typography>
        <Slider
          value={umbral}
          onChange={(_, v) => setUmbral(v)}
          min={50}
          max={99}
          sx={{ color: tokens.sello, mb: 2 }}
        />
        <Button
          variant="contained"
          startIcon={<HubOutlinedIcon />}
          onClick={handleGenerar}
          disabled={generando}
          sx={{ bgcolor: tokens.sello, '&:hover': { bgcolor: '#b3661f' } }}
        >
          {generando ? 'Ejecutando motor de agrupamiento…' : 'Generar nuevas propuestas'}
        </Button>
        {generando && <LinearProgress sx={{ mt: 2, borderRadius: 1 }} />}
      </Paper>

      {error && <Alert severity="warning" sx={{ mb: 3 }}>{error}</Alert>}

      {cargando ? (
        <LinearProgress sx={{ borderRadius: 1 }} />
      ) : propuestas.length === 0 ? (
        <Alert severity="info">No hay propuestas pendientes de validación en este momento.</Alert>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
          {propuestas.map((p) => (
            <Paper key={p.id} sx={{ p: 2.5, bgcolor: tokens.panel, borderLeft: `3px solid ${tokens.sello}` }}>
              <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 2 }}>
                <Box sx={{ flex: 1 }}>
                  <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.9rem', fontWeight: 600 }}>
                    {p.delitoRepresentativoSugerido}
                  </Typography>
                  <Box sx={{ display: 'flex', gap: 0.8, flexWrap: 'wrap', mt: 1 }}>
                    {p.ejemplosVariantes?.slice(0, 4).map((v, i) => (
                      <Chip key={i} size="small" label={v} sx={{ bgcolor: tokens.panelAlto }} />
                    ))}
                  </Box>
                </Box>
                <Box sx={{ textAlign: 'center', minWidth: 80 }}>
                  <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '1.2rem', fontWeight: 700, color: tokens.validado }}>
                    {p.cohesionPromedio}%
                  </Typography>
                  <Typography sx={{ fontSize: '0.62rem', color: tokens.textoTenue }}>cohesión</Typography>
                  <Chip size="small" label={`${p.cantidadVariantes} variantes`} sx={{ mt: 0.5, bgcolor: tokens.infoTenue, color: tokens.info }} />
                </Box>
              </Box>
            </Paper>
          ))}
        </Box>
      )}
    </Box>
  )
}

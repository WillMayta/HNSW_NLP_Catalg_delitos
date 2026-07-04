import { useEffect, useState } from 'react'
import { Box, Paper, Typography, Chip, LinearProgress, Alert, TextField, InputAdornment } from '@mui/material'
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined'
import SearchOutlinedIcon from '@mui/icons-material/SearchOutlined'
import { catalogoApi } from '../services/api.js'
import { tokens } from '../theme/theme.js'

const COLOR_ESTADO = {
  Borrador: tokens.textoTenue,
  PendienteValidacion: tokens.info,
  Validado: tokens.validado,
  Consolidado: tokens.validado,
  Rechazado: tokens.alerta,
}

export function PaginaCatalogoPenal() {
  const [delitos, setDelitos] = useState([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState('')
  const [filtro, setFiltro] = useState('')

  useEffect(() => {
    catalogoApi.obtenerTodos()
      .then(({ data }) => setDelitos(data))
      .catch(() => setError('No se pudo cargar el catálogo penal.'))
      .finally(() => setCargando(false))
  }, [])

  const delitosFiltrados = delitos.filter((d) =>
    d.nombreGenerico.toLowerCase().includes(filtro.toLowerCase())
  )

  return (
    <Box>
      <Box sx={{ mb: 4 }}>
        <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.68rem', color: tokens.sello, letterSpacing: '0.1em', mb: 0.5 }}>
          CATÁLOGO PENAL CONSOLIDADO
        </Typography>
        <Typography variant="h4" sx={{ fontSize: '1.6rem', mb: 1 }}>Catálogo de delitos genéricos</Typography>
        <Typography sx={{ fontSize: '0.85rem', color: tokens.textoTenue, mb: 2 }}>
          Construido progresivamente a partir de las propuestas validadas por especialistas
          jurídicos, conforme al Código Penal y legislación complementaria.
        </Typography>

        <TextField
          placeholder="Buscar delito genérico…"
          value={filtro}
          onChange={(e) => setFiltro(e.target.value)}
          size="small"
          sx={{ maxWidth: 360 }}
          InputProps={{
            startAdornment: <InputAdornment position="start"><SearchOutlinedIcon fontSize="small" sx={{ color: tokens.textoTenue }} /></InputAdornment>,
          }}
        />
      </Box>

      {error && <Alert severity="warning">{error}</Alert>}

      {cargando ? (
        <LinearProgress sx={{ borderRadius: 1 }} />
      ) : delitosFiltrados.length === 0 ? (
        <Alert severity="info">
          {delitos.length === 0
            ? 'Aún no hay delitos consolidados en el catálogo. Valide propuestas en el módulo de Validación Jurídica.'
            : 'No se encontraron delitos que coincidan con la búsqueda.'}
        </Alert>
      ) : (
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(280px, 1fr))', gap: 2 }}>
          {delitosFiltrados.map((d) => (
            <Paper key={d.id} className="borde-folio" sx={{ p: 2.5, bgcolor: tokens.panel }}>
              <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', mb: 1.5 }}>
                <MenuBookOutlinedIcon sx={{ color: tokens.sello, fontSize: 20 }} />
                <Chip
                  size="small"
                  label={d.estado}
                  sx={{ bgcolor: `${COLOR_ESTADO[d.estado] || tokens.textoTenue}22`, color: COLOR_ESTADO[d.estado] || tokens.textoTenue }}
                />
              </Box>
              <Typography sx={{ fontWeight: 600, fontSize: '0.9rem', mb: 0.5 }}>{d.nombreGenerico}</Typography>
              <Typography sx={{ fontSize: '0.74rem', color: tokens.textoTenue, mb: 1.5 }}>{d.familiaDelictiva}</Typography>
              <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                {d.articuloPrincipal && (
                  <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.72rem', color: tokens.info }}>
                    {d.articuloPrincipal}
                  </Typography>
                )}
                <Typography sx={{ fontSize: '0.72rem', color: tokens.textoTenue, ml: 'auto' }}>
                  {d.cantidadVariantes} variante{d.cantidadVariantes !== 1 ? 's' : ''}
                </Typography>
              </Box>
            </Paper>
          ))}
        </Box>
      )}
    </Box>
  )
}

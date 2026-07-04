import { useState } from 'react'
import {
  Box, Paper, Typography, TextField, Button, Chip, LinearProgress,
  Alert, InputAdornment,
} from '@mui/material'
import TravelExploreOutlinedIcon from '@mui/icons-material/TravelExploreOutlined'
import { busquedaApi } from '../services/api.js'
import { tokens } from '../theme/theme.js'

function colorSimilitud(porcentaje) {
  if (porcentaje >= 90) return tokens.validado
  if (porcentaje >= 75) return tokens.sello
  return tokens.alerta
}

export function PaginaBusqueda() {
  const [texto, setTexto] = useState('')
  const [resultado, setResultado] = useState(null)
  const [cargando, setCargando] = useState(false)
  const [error, setError] = useState('')

  const handleBuscar = async (e) => {
    e.preventDefault()
    if (!texto.trim()) return
    setCargando(true)
    setError('')
    try {
      const { data } = await busquedaApi.buscar(texto, 10)
      setResultado(data)
    } catch {
      setError('No se pudo completar la búsqueda. Verifique que el índice HNSW ya haya sido construido.')
    } finally {
      setCargando(false)
    }
  }

  return (
    <Box sx={{ maxWidth: 880 }}>
      <Box sx={{ mb: 4 }}>
        <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.68rem', color: tokens.sello, letterSpacing: '0.1em', mb: 0.5 }}>
          BÚSQUEDA VECTORIAL · HNSW
        </Typography>
        <Typography variant="h4" sx={{ fontSize: '1.6rem', mb: 1 }}>Búsqueda inteligente de delitos</Typography>
        <Typography sx={{ fontSize: '0.85rem', color: tokens.textoTenue }}>
          Ingrese la denominación de un delito tal como aparece en el expediente. El sistema
          normalizará el texto y buscará las variantes más similares en el catálogo histórico.
        </Typography>
      </Box>

      <Paper className="borde-folio" sx={{ p: 3, bgcolor: tokens.panel, mb: 3 }}>
        <Box component="form" onSubmit={handleBuscar} sx={{ display: 'flex', gap: 1.5 }}>
          <TextField
            fullWidth
            placeholder="Ej. ABUSO DE AUTORIDAD ART.376, C.F.P. (FALSIFICACION DOCUMENTARIA)…"
            value={texto}
            onChange={(e) => setTexto(e.target.value)}
            InputProps={{
              startAdornment: <InputAdornment position="start"><TravelExploreOutlinedIcon sx={{ color: tokens.textoTenue }} /></InputAdornment>,
            }}
          />
          <Button
            type="submit"
            variant="contained"
            disabled={cargando || !texto.trim()}
            sx={{ px: 4, bgcolor: tokens.sello, '&:hover': { bgcolor: '#b3661f' } }}
          >
            Buscar
          </Button>
        </Box>
        {cargando && <LinearProgress sx={{ mt: 2, borderRadius: 1 }} />}
      </Paper>

      {error && <Alert severity="warning" sx={{ mb: 3 }}>{error}</Alert>}

      {resultado && (
        <Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mb: 2, flexWrap: 'wrap' }}>
            <Typography sx={{ fontSize: '0.78rem', color: tokens.textoTenue }}>Texto normalizado:</Typography>
            <Chip
              label={resultado.textoNormalizado}
              sx={{ bgcolor: tokens.selloTenue, color: tokens.sello, fontFamily: 'IBM Plex Mono' }}
            />
          </Box>

          {resultado.delitosRelacionados?.length === 0 ? (
            <Alert severity="info">No se encontraron delitos similares en el índice vectorial.</Alert>
          ) : (
            <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
              {resultado.delitosRelacionados?.map((d, i) => {
                const color = colorSimilitud(d.porcentajeSimilitud)
                return (
                  <Paper key={i} sx={{ p: 2.5, bgcolor: tokens.panel, borderLeft: `3px solid ${color}` }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: 2 }}>
                      <Box sx={{ flex: 1 }}>
                        <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.85rem' }}>
                          {d.textoNormalizado}
                        </Typography>
                        <Box sx={{ display: 'flex', gap: 1, mt: 0.8, flexWrap: 'wrap' }}>
                          {d.familiaJuridica && (
                            <Chip size="small" label={d.familiaJuridica} sx={{ bgcolor: tokens.infoTenue, color: tokens.info }} />
                          )}
                          {d.delitoCatalogoAsociado && (
                            <Chip size="small" label={`Catálogo: ${d.delitoCatalogoAsociado}`} sx={{ bgcolor: tokens.validadoTenue, color: tokens.validado }} />
                          )}
                          {!d.delitoCatalogoAsociado && (
                            <Chip size="small" label="Sin consolidar aún" sx={{ bgcolor: tokens.alertaTenue, color: tokens.alerta }} />
                          )}
                        </Box>
                      </Box>
                      <Box sx={{ textAlign: 'center', minWidth: 64 }}>
                        <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '1.3rem', fontWeight: 700, color }}>
                          {d.porcentajeSimilitud}%
                        </Typography>
                        <Typography sx={{ fontSize: '0.62rem', color: tokens.textoTenue }}>similitud</Typography>
                      </Box>
                    </Box>
                  </Paper>
                )
              })}
            </Box>
          )}
        </Box>
      )}
    </Box>
  )
}

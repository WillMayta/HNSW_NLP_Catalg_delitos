import { useEffect, useState } from 'react'
import {
  Box, Paper, Typography, Button, Chip, Alert, LinearProgress,
  Dialog, DialogTitle, DialogContent, DialogActions, TextField,
  MenuItem, ToggleButtonGroup, ToggleButton,
} from '@mui/material'
import GavelOutlinedIcon from '@mui/icons-material/GavelOutlined'
import CheckCircleOutlineIcon from '@mui/icons-material/CheckCircleOutlineOutlined'
import HighlightOffIcon from '@mui/icons-material/HighlightOff'
import EditOutlinedIcon from '@mui/icons-material/EditOutlined'
import { agrupamientoApi, validacionApi } from '../services/api.js'
import { tokens } from '../theme/theme.js'

const FAMILIAS_DEMO = [
  'Delitos contra la vida, el cuerpo y la salud',
  'Delitos contra la libertad',
  'Delitos contra el patrimonio',
  'Delitos contra la familia',
  'Delitos contra la fe pública',
  'Delitos contra la administración pública',
]

export function PaginaValidacionJuridica() {
  const [propuestas, setPropuestas] = useState([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState('')
  const [mensajeExito, setMensajeExito] = useState('')
  const [propuestaActiva, setPropuestaActiva] = useState(null)
  const [decision, setDecision] = useState('Aprobar')
  const [nombreFinal, setNombreFinal] = useState('')
  const [familia, setFamilia] = useState('')
  const [articulo, setArticulo] = useState('')
  const [observaciones, setObservaciones] = useState('')
  const [enviando, setEnviando] = useState(false)

  const cargar = async () => {
    setCargando(true)
    try {
      const { data } = await agrupamientoApi.obtenerPendientes()
      setPropuestas(data)
    } catch {
      setError('No se pudieron cargar las propuestas pendientes de validación.')
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => { cargar() }, [])

  const abrirDialogo = (propuesta) => {
    setPropuestaActiva(propuesta)
    setDecision('Aprobar')
    setNombreFinal(propuesta.delitoRepresentativoSugerido)
    setFamilia('')
    setArticulo('')
    setObservaciones('')
  }

  const handleConfirmar = async () => {
    setEnviando(true)
    setError('')
    try {
      const payload = {
        propuestaId: propuestaActiva.id,
        decision,
        nombreGenericoFinal: decision === 'AprobarConModificaciones' ? nombreFinal : null,
        familiaDelictivaId: decision !== 'Rechazar' ? familia : null,
        articuloPrincipal: articulo || null,
        observaciones,
      }
      const { data } = await validacionApi.validar(payload)
      setMensajeExito(data.mensaje)
      setPropuestaActiva(null)
      await cargar()
    } catch (err) {
      setError(err.response?.data?.mensaje || err.response?.data?.title || 'Ocurrió un error al procesar la validación.')
    } finally {
      setEnviando(false)
    }
  }

  return (
    <Box sx={{ maxWidth: 900 }}>
      <Box sx={{ mb: 4 }}>
        <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.68rem', color: tokens.sello, letterSpacing: '0.1em', mb: 0.5 }}>
          CRITERIO JURÍDICO
        </Typography>
        <Typography variant="h4" sx={{ fontSize: '1.6rem', mb: 1 }}>Validación de propuestas</Typography>
        <Typography sx={{ fontSize: '0.85rem', color: tokens.textoTenue }}>
          Revise cada propuesta generada por el motor inteligente. Su decisión es la única vía
          para consolidar un delito en el Catálogo Penal oficial.
        </Typography>
      </Box>

      {mensajeExito && <Alert severity="success" sx={{ mb: 3 }} onClose={() => setMensajeExito('')}>{mensajeExito}</Alert>}
      {error && <Alert severity="error" sx={{ mb: 3 }} onClose={() => setError('')}>{error}</Alert>}

      {cargando ? (
        <LinearProgress sx={{ borderRadius: 1 }} />
      ) : propuestas.length === 0 ? (
        <Alert severity="info">No hay propuestas pendientes de validación jurídica.</Alert>
      ) : (
        <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
          {propuestas.map((p) => (
            <Paper key={p.id} className="borde-folio" sx={{ p: 2.5, bgcolor: tokens.panel }}>
              <Box sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: 2 }}>
                <Box sx={{ flex: 1 }}>
                  <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.9rem', fontWeight: 600 }}>
                    {p.delitoRepresentativoSugerido}
                  </Typography>
                  <Box sx={{ display: 'flex', gap: 0.8, flexWrap: 'wrap', mt: 1 }}>
                    {p.ejemplosVariantes?.map((v, i) => (
                      <Chip key={i} size="small" label={v} sx={{ bgcolor: tokens.panelAlto }} />
                    ))}
                  </Box>
                  <Typography sx={{ fontSize: '0.72rem', color: tokens.textoTenue, mt: 1 }}>
                    {p.cantidadVariantes} variantes · {p.cohesionPromedio}% de cohesión semántica
                  </Typography>
                </Box>
                <Button
                  variant="outlined"
                  startIcon={<GavelOutlinedIcon />}
                  onClick={() => abrirDialogo(p)}
                  sx={{ borderColor: tokens.sello, color: tokens.sello, whiteSpace: 'nowrap' }}
                >
                  Revisar
                </Button>
              </Box>
            </Paper>
          ))}
        </Box>
      )}

      <Dialog open={!!propuestaActiva} onClose={() => setPropuestaActiva(null)} maxWidth="sm" fullWidth>
        <DialogTitle sx={{ fontFamily: 'Source Serif 4' }}>Validación jurídica de propuesta</DialogTitle>
        <DialogContent sx={{ display: 'flex', flexDirection: 'column', gap: 2.5, pt: 1 }}>
          <ToggleButtonGroup
            value={decision}
            exclusive
            onChange={(_, v) => v && setDecision(v)}
            fullWidth
            size="small"
          >
            <ToggleButton value="Aprobar" sx={{ gap: 0.5 }}><CheckCircleOutlineIcon fontSize="small" /> Aprobar</ToggleButton>
            <ToggleButton value="AprobarConModificaciones" sx={{ gap: 0.5 }}><EditOutlinedIcon fontSize="small" /> Aprobar con cambios</ToggleButton>
            <ToggleButton value="Rechazar" sx={{ gap: 0.5 }}><HighlightOffIcon fontSize="small" /> Rechazar</ToggleButton>
          </ToggleButtonGroup>

          {decision !== 'Rechazar' && (
            <>
              {decision === 'AprobarConModificaciones' && (
                <TextField
                  label="Nombre genérico final"
                  value={nombreFinal}
                  onChange={(e) => setNombreFinal(e.target.value)}
                  fullWidth
                />
              )}
              <TextField
                select
                label="Familia delictiva"
                value={familia}
                onChange={(e) => setFamilia(e.target.value)}
                fullWidth
                required
                helperText="En producción, esta lista se carga desde /api/FamiliasDelictivas"
              >
                {FAMILIAS_DEMO.map((f) => (
                  <MenuItem key={f} value={f}>{f}</MenuItem>
                ))}
              </TextField>
              <TextField
                label="Artículo principal del Código Penal (opcional)"
                value={articulo}
                onChange={(e) => setArticulo(e.target.value)}
                fullWidth
                placeholder="Ej. Art. 376"
              />
            </>
          )}

          <TextField
            label="Observaciones jurídicas"
            value={observaciones}
            onChange={(e) => setObservaciones(e.target.value)}
            fullWidth
            multiline
            minRows={2}
          />
        </DialogContent>
        <DialogActions sx={{ p: 2.5 }}>
          <Button onClick={() => setPropuestaActiva(null)}>Cancelar</Button>
          <Button
            variant="contained"
            disabled={enviando || (decision !== 'Rechazar' && !familia)}
            onClick={handleConfirmar}
            sx={{ bgcolor: tokens.sello, '&:hover': { bgcolor: '#b3661f' } }}
          >
            {enviando ? 'Procesando…' : 'Confirmar decisión'}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}

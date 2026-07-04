import { useState, useRef } from 'react'
import {
  Box, Paper, Typography, Button, LinearProgress, Alert, Chip,
  List, ListItem, ListItemText, Divider,
} from '@mui/material'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import InsertDriveFileOutlinedIcon from '@mui/icons-material/InsertDriveFileOutlined'
import { normalizacionApi } from '../services/api.js'
import { tokens } from '../theme/theme.js'

export function PaginaCargaMasiva() {
  const [archivo, setArchivo] = useState(null)
  const [progreso, setProgreso] = useState(0)
  const [cargando, setCargando] = useState(false)
  const [resultado, setResultado] = useState(null)
  const [error, setError] = useState('')
  const inputRef = useRef(null)

  const handleSeleccionar = (e) => {
    setResultado(null)
    setError('')
    setArchivo(e.target.files[0] || null)
  }

  const handleCargar = async () => {
    if (!archivo) return
    setCargando(true)
    setError('')
    setProgreso(0)
    try {
      const { data } = await normalizacionApi.cargaMasiva(archivo, (evt) => {
        setProgreso(Math.round((evt.loaded * 100) / evt.total))
      })
      setResultado(data)
    } catch (err) {
      setError(err.response?.data?.mensaje || 'Ocurrió un error al procesar la carga. Verifique el formato del archivo.')
    } finally {
      setCargando(false)
    }
  }

  return (
    <Box sx={{ maxWidth: 760 }}>
      <Box sx={{ mb: 4 }}>
        <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.68rem', color: tokens.sello, letterSpacing: '0.1em', mb: 0.5 }}>
          CARGA HISTÓRICA
        </Typography>
        <Typography variant="h4" sx={{ fontSize: '1.6rem', mb: 1 }}>Carga de expedientes</Typography>
        <Typography sx={{ fontSize: '0.85rem', color: tokens.textoTenue }}>
          Suba el archivo Excel o CSV con los registros históricos. El sistema ejecutará
          automáticamente la normalización léxica y la generación de embeddings sobre cada
          denominación de delito cargada.
        </Typography>
      </Box>

      <Paper className="borde-folio" sx={{ p: 4, bgcolor: tokens.panel, mb: 3 }}>
        <Box
          sx={{
            border: `1.5px dashed ${tokens.borde}`, borderRadius: 2, p: 5,
            display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1.5,
            cursor: 'pointer', transition: 'border-color 0.15s',
            '&:hover': { borderColor: tokens.sello },
          }}
          onClick={() => inputRef.current?.click()}
        >
          <UploadFileOutlinedIcon sx={{ fontSize: 36, color: tokens.textoTenue }} />
          <Typography sx={{ fontSize: '0.88rem', fontWeight: 600 }}>
            {archivo ? 'Archivo seleccionado' : 'Haga clic para seleccionar el archivo'}
          </Typography>
          <Typography sx={{ fontSize: '0.75rem', color: tokens.textoTenue }}>
            Formatos admitidos: .xlsx, .csv — Hasta ~50,000 registros
          </Typography>
          <input ref={inputRef} type="file" hidden accept=".xlsx,.xls,.csv" onChange={handleSeleccionar} />
        </Box>

        {archivo && (
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.5, mt: 2.5, p: 1.5, bgcolor: tokens.panelAlto, borderRadius: 1 }}>
            <InsertDriveFileOutlinedIcon sx={{ color: tokens.sello, fontSize: 20 }} />
            <Typography sx={{ fontSize: '0.8rem', flex: 1, fontFamily: 'IBM Plex Mono' }}>{archivo.name}</Typography>
            <Chip size="small" label={`${(archivo.size / 1024).toFixed(0)} KB`} />
          </Box>
        )}

        {cargando && (
          <Box sx={{ mt: 2.5 }}>
            <LinearProgress variant="determinate" value={progreso} sx={{ borderRadius: 1, height: 6 }} />
            <Typography sx={{ fontSize: '0.72rem', color: tokens.textoTenue, mt: 0.8 }}>
              Subiendo y procesando… {progreso}%
            </Typography>
          </Box>
        )}

        <Button
          variant="contained"
          fullWidth
          size="large"
          disabled={!archivo || cargando}
          onClick={handleCargar}
          sx={{ mt: 3, py: 1.3, bgcolor: tokens.sello, '&:hover': { bgcolor: '#b3661f' } }}
        >
          {cargando ? 'Procesando…' : 'Iniciar carga y normalización'}
        </Button>
      </Paper>

      {error && <Alert severity="error" sx={{ mb: 3 }}>{error}</Alert>}

      {resultado && (
        <Paper className="borde-folio" sx={{ p: 3, bgcolor: tokens.panel }}>
          <Typography sx={{ fontSize: '0.85rem', fontWeight: 600, mb: 2 }}>Resultado de la carga</Typography>
          <Box sx={{ display: 'flex', gap: 1.5, flexWrap: 'wrap', mb: 2 }}>
            <Chip label={`Total: ${resultado.totalRegistros}`} sx={{ bgcolor: tokens.infoTenue, color: tokens.info }} />
            <Chip label={`Exitosos: ${resultado.registrosExitosos}`} sx={{ bgcolor: tokens.validadoTenue, color: tokens.validado }} />
            <Chip label={`Con error: ${resultado.registrosConError}`} sx={{ bgcolor: tokens.alertaTenue, color: tokens.alerta }} />
          </Box>
          {resultado.errores?.length > 0 && (
            <>
              <Divider sx={{ my: 1.5 }} />
              <Typography sx={{ fontSize: '0.78rem', color: tokens.textoTenue, mb: 1 }}>Detalle de errores:</Typography>
              <List dense sx={{ maxHeight: 200, overflow: 'auto' }}>
                {resultado.errores.map((e, i) => (
                  <ListItem key={i} sx={{ py: 0.3 }}>
                    <ListItemText primaryTypographyProps={{ fontSize: '0.75rem', color: tokens.alerta }} primary={e} />
                  </ListItem>
                ))}
              </List>
            </>
          )}
        </Paper>
      )}
    </Box>
  )
}

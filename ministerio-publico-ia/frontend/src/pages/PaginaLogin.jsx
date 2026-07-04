import { useState } from 'react'
import { useNavigate, useLocation } from 'react-router-dom'
import { Box, Paper, Typography, TextField, Button, Alert, InputAdornment } from '@mui/material'
import GavelOutlinedIcon from '@mui/icons-material/GavelOutlined'
import PersonOutlineOutlinedIcon from '@mui/icons-material/PersonOutlineOutlined'
import LockOutlinedIcon from '@mui/icons-material/LockOutlined'
import { useAuth } from '../context/AuthContext.jsx'
import { tokens } from '../theme/theme.js'

export function PaginaLogin() {
  const { iniciarSesion } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [nombreUsuario, setNombreUsuario] = useState('')
  const [contrasena, setContrasena] = useState('')
  const [error, setError] = useState('')
  const [cargando, setCargando] = useState(false)

  const handleSubmit = async (e) => {
    e.preventDefault()
    setError('')
    setCargando(true)
    try {
      await iniciarSesion(nombreUsuario, contrasena)
      navigate(location.state?.from || '/', { replace: true })
    } catch {
      setError('Usuario o contraseña incorrectos. Verifique sus credenciales institucionales.')
    } finally {
      setCargando(false)
    }
  }

  return (
    <Box sx={{
      minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center',
      bgcolor: tokens.fondo, px: 2,
      backgroundImage: `radial-gradient(circle at 50% 0%, ${tokens.selloTenue}, transparent 50%)`,
    }}>
      <Paper className="borde-folio" sx={{ width: '100%', maxWidth: 420, p: 5, bgcolor: tokens.panel }}>
        <Box sx={{ textAlign: 'center', mb: 4 }}>
          <Box sx={{
            width: 56, height: 56, borderRadius: '8px', mx: 'auto', mb: 2,
            bgcolor: tokens.selloTenue, border: `1px solid ${tokens.sello}`,
            display: 'flex', alignItems: 'center', justifyContent: 'center',
          }}>
            <GavelOutlinedIcon sx={{ fontSize: 28, color: tokens.sello }} />
          </Box>
          <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.68rem', color: tokens.textoTenue, letterSpacing: '0.1em', mb: 0.5 }}>
            MINISTERIO PÚBLICO DEL PERÚ
          </Typography>
          <Typography variant="h4" sx={{ fontSize: '1.5rem', mb: 0.5 }}>
            Catálogo Penal Inteligente
          </Typography>
          <Typography sx={{ fontSize: '0.8rem', color: tokens.textoTenue }}>
            Normalización, agrupación y consolidación del catálogo de delitos
          </Typography>
        </Box>

        {error && <Alert severity="error" sx={{ mb: 2.5 }}>{error}</Alert>}

        <Box component="form" onSubmit={handleSubmit} sx={{ display: 'flex', flexDirection: 'column', gap: 2.5 }}>
          <TextField
            label="Usuario institucional"
            value={nombreUsuario}
            onChange={(e) => setNombreUsuario(e.target.value)}
            fullWidth
            required
            autoFocus
            InputProps={{
              startAdornment: <InputAdornment position="start"><PersonOutlineOutlinedIcon fontSize="small" sx={{ color: tokens.textoTenue }} /></InputAdornment>,
            }}
          />
          <TextField
            label="Contraseña"
            type="password"
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
            fullWidth
            required
            InputProps={{
              startAdornment: <InputAdornment position="start"><LockOutlinedIcon fontSize="small" sx={{ color: tokens.textoTenue }} /></InputAdornment>,
            }}
          />
          <Button
            type="submit"
            variant="contained"
            size="large"
            disabled={cargando}
            sx={{ py: 1.3, bgcolor: tokens.sello, '&:hover': { bgcolor: '#b3661f' } }}
          >
            {cargando ? 'Verificando credenciales…' : 'Ingresar al sistema'}
          </Button>
        </Box>

        <Typography sx={{ fontSize: '0.7rem', color: tokens.textoTenue, textAlign: 'center', mt: 3 }}>
          Acceso restringido a personal autorizado del Ministerio Público.
          Todas las sesiones quedan registradas en el módulo de auditoría.
        </Typography>
      </Paper>
    </Box>
  )
}

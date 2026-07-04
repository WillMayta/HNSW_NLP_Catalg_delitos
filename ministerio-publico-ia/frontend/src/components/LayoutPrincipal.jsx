import { useState } from 'react'
import { NavLink, Outlet, useNavigate } from 'react-router-dom'
import {
  Box, Drawer, AppBar, Toolbar, Typography, List, ListItemButton,
  ListItemIcon, ListItemText, IconButton, Avatar, Menu, MenuItem,
  Divider, Chip,
} from '@mui/material'
import DashboardOutlinedIcon from '@mui/icons-material/DashboardOutlined'
import UploadFileOutlinedIcon from '@mui/icons-material/UploadFileOutlined'
import TravelExploreOutlinedIcon from '@mui/icons-material/TravelExploreOutlined'
import HubOutlinedIcon from '@mui/icons-material/HubOutlined'
import GavelOutlinedIcon from '@mui/icons-material/GavelOutlined'
import MenuBookOutlinedIcon from '@mui/icons-material/MenuBookOutlined'
import LogoutOutlinedIcon from '@mui/icons-material/LogoutOutlined'
import { useAuth } from '../context/AuthContext.jsx'
import { tokens } from '../theme/theme.js'

const ANCHO_DRAWER = 248

const ITEMS_NAV = [
  { texto: 'Panel institucional', ruta: '/', icono: DashboardOutlinedIcon },
  { texto: 'Carga de expedientes', ruta: '/carga', icono: UploadFileOutlinedIcon, roles: ['Administrador', 'Analista'] },
  { texto: 'Búsqueda inteligente', ruta: '/busqueda', icono: TravelExploreOutlinedIcon },
  { texto: 'Propuestas de agrupación', ruta: '/agrupamiento', icono: HubOutlinedIcon, roles: ['Administrador', 'Analista'] },
  { texto: 'Validación jurídica', ruta: '/validacion', icono: GavelOutlinedIcon, roles: ['EspecialistaJuridico', 'Administrador'] },
  { texto: 'Catálogo penal', ruta: '/catalogo', icono: MenuBookOutlinedIcon },
]

export function LayoutPrincipal() {
  const { usuario, cerrarSesion, tieneRol } = useAuth()
  const navigate = useNavigate()
  const [anclaMenu, setAnclaMenu] = useState(null)

  const itemsVisibles = ITEMS_NAV.filter((item) => !item.roles || tieneRol(...item.roles))

  const handleCerrarSesion = () => {
    cerrarSesion()
    navigate('/login')
  }

  return (
    <Box sx={{ display: 'flex', minHeight: '100vh', bgcolor: tokens.fondo }}>
      <Drawer
        variant="permanent"
        sx={{
          width: ANCHO_DRAWER,
          flexShrink: 0,
          [`& .MuiDrawer-paper`]: {
            width: ANCHO_DRAWER, boxSizing: 'border-box',
            bgcolor: tokens.panel, borderRight: `1px solid ${tokens.borde}`,
          },
        }}
      >
        <Box sx={{ px: 2.5, py: 3, borderBottom: `1px solid ${tokens.borde}` }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.2 }}>
            <Box sx={{
              width: 34, height: 34, borderRadius: '4px',
              bgcolor: tokens.selloTenue, border: `1px solid ${tokens.sello}`,
              display: 'flex', alignItems: 'center', justifyContent: 'center',
            }}>
              <GavelOutlinedIcon sx={{ fontSize: 18, color: tokens.sello }} />
            </Box>
            <Box>
              <Typography sx={{ fontFamily: 'IBM Plex Mono', fontSize: '0.62rem', color: tokens.textoTenue, letterSpacing: '0.08em' }}>
                MINISTERIO PÚBLICO · PERÚ
              </Typography>
              <Typography variant="h6" sx={{ fontSize: '0.92rem', lineHeight: 1.2 }}>
                Catálogo Penal IA
              </Typography>
            </Box>
          </Box>
        </Box>

        <List sx={{ px: 1.5, py: 2, flex: 1 }}>
          {itemsVisibles.map(({ texto, ruta, icono: Icono }) => (
            <ListItemButton
              key={ruta}
              component={NavLink}
              to={ruta}
              end={ruta === '/'}
              sx={{
                borderRadius: 1, mb: 0.5, color: tokens.textoTenue,
                '&.active': {
                  bgcolor: tokens.selloTenue, color: tokens.texto,
                  borderLeft: `2px solid ${tokens.sello}`,
                  '& .MuiListItemIcon-root': { color: tokens.sello },
                },
                '&:hover': { bgcolor: tokens.panelAlto },
              }}
            >
              <ListItemIcon sx={{ minWidth: 36, color: 'inherit' }}>
                <Icono fontSize="small" />
              </ListItemIcon>
              <ListItemText primaryTypographyProps={{ fontSize: '0.84rem', fontWeight: 500 }}>
                {texto}
              </ListItemText>
            </ListItemButton>
          ))}
        </List>

        <Box sx={{ p: 2, borderTop: `1px solid ${tokens.borde}` }}>
          <Chip
            size="small"
            label="Apoyo a la decisión · No reemplaza criterio fiscal"
            sx={{ bgcolor: tokens.infoTenue, color: tokens.info, width: '100%', height: 'auto', py: 0.7, whiteSpace: 'normal', '& .MuiChip-label': { whiteSpace: 'normal', textAlign: 'center', lineHeight: 1.3 } }}
          />
        </Box>
      </Drawer>

      <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
        <AppBar
          position="sticky"
          elevation={0}
          sx={{ bgcolor: tokens.panel, borderBottom: `1px solid ${tokens.borde}`, color: tokens.texto }}
        >
          <Toolbar sx={{ justifyContent: 'flex-end', gap: 1.5 }}>
            <Box sx={{ textAlign: 'right' }}>
              <Typography sx={{ fontSize: '0.82rem', fontWeight: 600 }}>{usuario?.nombreCompleto}</Typography>
              <Typography sx={{ fontSize: '0.68rem', color: tokens.textoTenue }}>
                {usuario?.roles?.join(' · ')}
              </Typography>
            </Box>
            <IconButton onClick={(e) => setAnclaMenu(e.currentTarget)} size="small">
              <Avatar sx={{ width: 32, height: 32, bgcolor: tokens.sello, fontSize: '0.85rem', color: '#0D0A06' }}>
                {usuario?.nombreCompleto?.charAt(0) || 'U'}
              </Avatar>
            </IconButton>
            <Menu anchorEl={anclaMenu} open={!!anclaMenu} onClose={() => setAnclaMenu(null)}>
              <MenuItem onClick={handleCerrarSesion}>
                <LogoutOutlinedIcon fontSize="small" sx={{ mr: 1 }} /> Cerrar sesión
              </MenuItem>
            </Menu>
          </Toolbar>
        </AppBar>

        <Box sx={{ flex: 1, p: { xs: 2, md: 4 } }}>
          <Outlet />
        </Box>
      </Box>
    </Box>
  )
}

import { createTheme } from '@mui/material/styles'

/*
  Sistema de diseño "Archivo Penal" — identidad visual propia para el
  Catálogo Inteligente de Delitos del Ministerio Público.

  Paleta:
    --fondo:        #0D1117  (papel oscuro de archivo)
    --panel:        #161B22  (carpeta / folder)
    --panel-alto:   #1C2330
    --borde:        #2A3140
    --texto:        #E6E8EB
    --texto-tenue:  #8B94A3
    --sello:        #C9762C  (bronce / sello oficial)
    --sello-tenue:  #C9762C22
    --validado:     #2D6A4F  (verde jurídico de validación)
    --alerta:       #B8453A  (rojo de inconsistencia / rechazo)
    --info:         #3D6FA8  (azul de búsqueda / IA)

  Tipografía:
    Display / títulos:  "Source Serif 4"   (evoca el Código Penal impreso)
    UI / cuerpo:         "IBM Plex Sans"
    Datos / folios:      "IBM Plex Mono"
*/

export const tokens = {
  fondo: '#0D1117',
  panel: '#161B22',
  panelAlto: '#1C2330',
  borde: '#2A3140',
  texto: '#E6E8EB',
  textoTenue: '#8B94A3',
  sello: '#C9762C',
  selloTenue: 'rgba(201,118,44,0.14)',
  validado: '#2D6A4F',
  validadoTenue: 'rgba(45,106,79,0.16)',
  alerta: '#B8453A',
  alertaTenue: 'rgba(184,69,58,0.16)',
  info: '#3D6FA8',
  infoTenue: 'rgba(61,111,168,0.16)',
}

export const theme = createTheme({
  palette: {
    mode: 'dark',
    background: { default: tokens.fondo, paper: tokens.panel },
    primary: { main: tokens.sello, contrastText: '#0D0A06' },
    secondary: { main: tokens.info },
    success: { main: tokens.validado },
    error: { main: tokens.alerta },
    text: { primary: tokens.texto, secondary: tokens.textoTenue },
    divider: tokens.borde,
  },
  typography: {
    fontFamily: '"IBM Plex Sans", "Segoe UI", sans-serif',
    h1: { fontFamily: '"Source Serif 4", Georgia, serif', fontWeight: 600 },
    h2: { fontFamily: '"Source Serif 4", Georgia, serif', fontWeight: 600 },
    h3: { fontFamily: '"Source Serif 4", Georgia, serif', fontWeight: 600 },
    h4: { fontFamily: '"Source Serif 4", Georgia, serif', fontWeight: 600 },
    h5: { fontFamily: '"Source Serif 4", Georgia, serif', fontWeight: 600 },
    h6: { fontFamily: '"IBM Plex Sans", sans-serif', fontWeight: 600, letterSpacing: '0.02em' },
    button: { textTransform: 'none', fontWeight: 600 },
  },
  shape: { borderRadius: 6 },
  components: {
    MuiPaper: {
      styleOverrides: {
        root: {
          backgroundImage: 'none',
          border: `1px solid ${tokens.borde}`,
        },
      },
    },
    MuiButton: {
      styleOverrides: {
        root: { borderRadius: 4 },
      },
    },
    MuiChip: {
      styleOverrides: {
        root: { borderRadius: 4, fontFamily: '"IBM Plex Mono", monospace', fontSize: '0.72rem' },
      },
    },
  },
})

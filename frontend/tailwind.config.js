/** @type {import('tailwindcss').Config} */
/*
 * Configura el tema de Tailwind CSS.
 * Las claves darkMode, content, theme, extend, colors, boxShadow y plugins
 * son nombres reservados por la biblioteca y deben mantenerse en inglés.
 */
export default {
  darkMode: 'class',
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      fontFamily: { sans: ['Inter', 'ui-sans-serif', 'system-ui', 'sans-serif'] },
      // Colores de marca: dorado, crema, marfil y tonos de contraste.
      colors: {
        brand: { 50: '#FEFBEA', 100: '#F6F0B3', 200:'#EADF8C', 300:'#DCC763',400:'#D4AF36',500:'#CD9901',600:'#CD9901',700:'#886600',800:'#765600',900:'#554000' },
        blue: { 50: '#FEFBEA', 100: '#F6F0B3', 200:'#EADF8C', 300:'#DCC763', 400:'#D4AF36', 500:'#CD9901', 600:'#CD9901', 700:'#886600', 800:'#765600', 900:'#554000', 950:'#382B10' },
        slate: {50:'#F2F2F2',100:'#F2F0E9',200:'#E5DEC8',300:'#D1C6AD',400:'#8B806C',500:'#685E4C',600:'#594D3E',700:'#40362B',800:'#342D23',900:'#242018',950:'#171510'},
        sky: {50:'#FEFBEA',200:'#EADF8C',500:'#D4AF36',700:'#8B6800',950:'#392C13'},
        violet: {50:'#F6F0B3',200:'#EADF8C',500:'#B18A23',600:'#AA7B0C',700:'#765600',950:'#423316'},
        emerald: {50:'#F6F0B3',200:'#EADF8C',300:'#D4AF36',400:'#D4AF36',500:'#B89122',600:'#B89122',700:'#765600',800:'#624900',950:'#403316'},
        green: {100:'#F6F0B3',200:'#EADF8C',600:'#9F812C',800:'#644C12',950:'#3F321A'},
        amber: {50:'#FEFBEA',200:'#F6F0B3',300:'#EADF8C',600:'#AE8408',700:'#806000',800:'#654A00',900:'#4C3B1A',950:'#352B17'},
        rose: {50:'#EFE3C7',200:'#DBC6AA',500:'#AA774A',700:'#755130',950:'#402E22'}
      },
      // Sombra personalizada para paneles y tarjetas.
      boxShadow: {suave:'0 2px 18px rgba(69,51,13,.07)'}
    }
  },
  plugins: []
}

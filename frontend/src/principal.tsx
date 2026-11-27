/**
 * NexTalent — principal
 * Monta la aplicación React dentro del elemento principal de la página.
 * Los nombres propios están en español; React, HTML y Tailwind mantienen su sintaxis oficial.
 */
import React from 'react';
import ReactDOM from 'react-dom/client';
import Aplicacion from './Aplicacion';
import { ProveedorAplicacion } from './contexto/ContextoAplicacion';
import './estilos.css';

ReactDOM.createRoot(document.getElementById('aplicacion')!).render(
  <React.StrictMode><ProveedorAplicacion><Aplicacion /></ProveedorAplicacion></React.StrictMode>
);

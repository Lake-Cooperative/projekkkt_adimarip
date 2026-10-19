import React from 'react';
import ReactDOM from 'react-dom/client';
import { RegistryApp } from './registry/RegistryApp';
import './styles.css';

ReactDOM.createRoot(document.getElementById('root')!).render(
  <React.StrictMode>
    <RegistryApp />
  </React.StrictMode>,
);

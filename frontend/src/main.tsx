import React from 'react';
import ReactDOM from 'react-dom/client';
import { RegistryApp } from './app/RegistryApp';
import './styles/base.css';
import './styles/registry.css';

const root = document.getElementById('root');
if (!root) throw new Error('В index.html нет элемента #root');

ReactDOM.createRoot(root).render(
  <React.StrictMode>
    <RegistryApp />
  </React.StrictMode>,
);

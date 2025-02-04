import ReactDOM from "react-dom/client";
import { BrowserRouter } from "react-router-dom";

import "./index.css";
import App from "./App.tsx";
import "bootstrap/dist/css/bootstrap.min.css";
import React from "react";
import { AxiosApiInterceptor } from "./utils/AxiosApiInterceptor.ts";

// Root rendering
const root = ReactDOM.createRoot(document.getElementById("root")!);

const renderApp = () => (
  <React.StrictMode>
    <BrowserRouter>
      <AxiosApiInterceptor />
      <App />
    </BrowserRouter>
  </React.StrictMode>
);

root.render(renderApp());
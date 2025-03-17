import React from "react";
import { BrowserRouter as Router, Routes, Route } from "react-router-dom";
import AuthPage from "./pages/AuthPage";
import ShortenPage from "./pages/ShortenPage";
import MyUrlsPage from "./pages/MyUrlsPage";

function App() {
  return (
    <Router>
      <Routes>
        <Route path="/" element={<AuthPage />} />
        <Route path="/shorten" element={<ShortenPage />} />
        <Route path="/my-urls" element={<MyUrlsPage />} />
      </Routes>
    </Router>
  );
}

export default App;

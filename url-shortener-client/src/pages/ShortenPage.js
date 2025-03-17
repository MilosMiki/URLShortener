import React, { useState, useEffect } from "react";
import { supabase } from "../supabaseClient";
import { Link, useNavigate } from "react-router-dom";

function ShortenPage() {
  const [url, setUrl] = useState("");
  const [shortenedUrl, setShortenedUrl] = useState("");
  const navigate = useNavigate();

  useEffect(() => {
    const checkAuth = async () => {
      const { data: { session } } = await supabase.auth.getSession();
      if (!session) {
        navigate("/"); // back to login page if unauthorized
      }
    };
    checkAuth();
  }, [navigate]);
  const handleShorten = async () => {
    const { data: { session } } = await supabase.auth.getSession();
    if (!session) {
      navigate("/");
      return;
    }
  
    const token = session.access_token;
  
    try {
      const response = await fetch("http://localhost:5000/api/shorten", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          "Authorization": `Bearer ${token}`
        },
        body: JSON.stringify({ url }),
      });
  
      if (!response.ok) throw new Error("Failed to shorten URL");
  
      const result = await response.json();
      setShortenedUrl(`http://localhost:5000/${result.code}`);
    } catch (error) {
      console.error("Error:", error);
      alert(error.message || "An unexpected error occurred");
    }
  };

  return (
    <div style={{ textAlign: "center", marginTop: "50px" }}>
      <h2>Shorten a URL</h2>
      <input
        type="text"
        placeholder="Enter a URL"
        value={url}
        onChange={(e) => setUrl(e.target.value)}
      />
      <button onClick={handleShorten}>Shorten</button>
      {shortenedUrl && <p>Shortened URL: <a href={shortenedUrl}>{shortenedUrl}</a></p>}
      <p><Link to="/my-urls">View My URLs</Link></p>
    </div>
  );
}

export default ShortenPage;

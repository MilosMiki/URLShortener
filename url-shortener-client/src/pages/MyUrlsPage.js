import React, { useState, useEffect } from "react";
import { supabase } from "../supabaseClient";
import { Link, useNavigate } from "react-router-dom";

function MyUrlsPage() {
  const [urls, setUrls] = useState([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    const fetchUrls = async () => {
      const { data: { session } } = await supabase.auth.getSession();
      if (!session) {
        navigate("/");
        return;
      }

      const token = session.access_token;
      const email = session.user.email;

      try {
        const response = await fetch(`http://localhost:5000/api/my-urls`, {
          method: "GET",
          headers: {
            "Authorization": `Bearer ${token}`,
            "Content-Type": "application/json"
          }
        });

        if (!response.ok) throw new Error("Failed to fetch URLs");

        const result = await response.json();
        setUrls(result);
      } catch (error) {
        console.error("Error:", error);
        alert(error.message || "An unexpected error occurred");
      } finally {
        setLoading(false);
      }
    };

    fetchUrls();
  }, [navigate]);

  const handleRefresh = async () => {
    setLoading(true);
    window.location.reload();
  };

  return (
    <div style={{ textAlign: "center", marginTop: "50px" }}>
      <h2>My Shortened URLs</h2>
      <button onClick={handleRefresh}>Refresh</button>
      {loading ? (
        <p>Loading...</p>
      ) : urls.length === 0 ? (
        <p>No URLs found.</p>
      ) : (
        <table border="1" style={{ margin: "20px auto", width: "80%" }}>
          <thead>
            <tr>
              <th>Short Code</th>
              <th>Full URL</th>
              <th>Access Count</th>
            </tr>
          </thead>
          <tbody>
            {urls.map((entry) => (
              <tr key={entry.id}>
                <td>
                  <a href={`http://localhost:5000/${entry.id}`} target="_blank" rel="noopener noreferrer">
                    {entry.id}
                  </a>
                </td>
                <td>{entry.full_url}</td>
                <td>{entry.access_count}</td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
      <p><Link to="/shorten">Back</Link></p>
    </div>
  );
}

export default MyUrlsPage;

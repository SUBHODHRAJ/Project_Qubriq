import { useEffect, useState } from "react";
import * as THREE from "three";

function App() {
    const [apiStatus, setApiStatus] = useState("Checking API...");
    const [apiDetails, setApiDetails] = useState(null);

    useEffect(() => {
        fetch("/api/health")
            .then(response => {
                if (!response.ok) {
                    throw new Error("API request failed");
                }
                return response.json();
            })
            .then(data => {
                setApiStatus("Connected");
                setApiDetails(data);
            })
            .catch(() => {
                setApiStatus("Not connected");
            });
    }, []);

    return (
        <main className="app">
            <section className="panel">
                <div className="brand">QUBRIQ</div>
                <p className="subtitle">Logistics Operating System</p>

                <h1>Project Foundation</h1>

                <p className="description">
                    Intelligent Logistics. Complete Transparency.
                </p>

                <div className="status-card">
                    <span>Frontend</span>
                    <strong className="success">React application running</strong>
                </div>

                <div className="status-card">
                    <span>Three.js</span>
                    <strong className="success">
                        Version {THREE.REVISION}
                    </strong>
                </div>

                <div className="status-card">
                    <span>Backend API</span>
                    <strong className={
                        apiStatus === "Connected" ? "success" : "warning"
                    }>
                        {apiStatus}
                    </strong>
                </div>

                {apiDetails && (
                    <pre className="api-result">
                        {JSON.stringify(apiDetails, null, 2)}
                    </pre>
                )}

                <p className="footer">
                    Team 3 | Qubriq Development | Stage 0
                </p>
            </section>
        </main>
    );
}

export default App;

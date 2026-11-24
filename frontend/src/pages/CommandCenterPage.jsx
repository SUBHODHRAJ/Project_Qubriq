import { useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { dispatcherService } from "../services/dispatcherService";
import "../styles/dashboard.css";
import "../styles/dispatcher.css";

function CommandCenterPage() {
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    useEffect(() => {
        let active = true;

        async function load() {
            try {
                const result = await dispatcherService.getDashboard();
                if (active) setData(result);
            } catch (err) {
                if (active) setError(err.message || "Dashboard could not be loaded.");
            } finally {
                if (active) setLoading(false);
            }
        }

        load();
        return () => { active = false; };
    }, []);

    const metrics = [
        ["Total bookings", data?.bookings?.total ?? 0],
        ["Pending bookings", data?.bookings?.pending ?? 0],
        ["Confirmed bookings", data?.bookings?.confirmed ?? 0],
        ["Active shipments", data?.shipments?.active ?? 0],
        ["Delivered shipments", data?.shipments?.delivered ?? 0],
        ["Available vehicles", data?.fleet?.availableVehicles ?? 0],
        ["Available drivers", data?.fleet?.availableDrivers ?? 0],
        ["Active assignments", data?.fleet?.activeAssignments ?? 0]
    ];

    return (
        <section className="module-page dispatcher-page">
            <div className="page-heading-row">
                <div>
                    <h1>Command Center</h1>
                    <p>Live operational overview of Qubriq logistics.</p>
                </div>
                <div className="page-actions">
                    <Link to="/app/bookings" className="button-secondary">
                        Manage bookings
                    </Link>
                    <Link to="/app/shipments" className="button-primary">
                        Manage shipments
                    </Link>
                </div>
            </div>

            {loading && <div className="dashboard-card dispatcher-message">Loading dashboard metrics...</div>}
            {error && <div className="dispatcher-error" role="alert">{error}</div>}

            {!loading && !error && (
                <>
                    <div className="dispatcher-metric-grid">
                        {metrics.map(([label, value]) => (
                            <article className="dashboard-card dispatcher-metric" key={label}>
                                <span>{label}</span>
                                <strong>{value}</strong>
                            </article>
                        ))}
                    </div>

                    <div className="dispatcher-section-grid">
                        <article className="dashboard-card dispatcher-panel">
                            <h2>Booking overview</h2>
                            <p>Pending: <strong>{data?.bookings?.pending ?? 0}</strong></p>
                            <p>Confirmed: <strong>{data?.bookings?.confirmed ?? 0}</strong></p>
                            <p>Rejected: <strong>{data?.bookings?.rejected ?? 0}</strong></p>
                            <Link to="/app/bookings">Open booking management →</Link>
                        </article>

                        <article className="dashboard-card dispatcher-panel">
                            <h2>Fleet availability</h2>
                            <p>Vehicles available: <strong>{data?.fleet?.availableVehicles ?? 0}</strong></p>
                            <p>Drivers available: <strong>{data?.fleet?.availableDrivers ?? 0}</strong></p>
                            <p>Active assignments: <strong>{data?.fleet?.activeAssignments ?? 0}</strong></p>
                            <Link to="/app/fleet">Open fleet planning →</Link>
                        </article>
                    </div>

                    <p className="dispatcher-updated">
                        Last loaded: {data?.generatedAtUtc
                            ? new Date(data.generatedAtUtc).toLocaleString()
                            : "—"}
                    </p>
                </>
            )}
        </section>
    );
}

export default CommandCenterPage;

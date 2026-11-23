import { Link } from "react-router-dom";

const metricCards = [
    "Active shipments",
    "Vehicles available",
    "Open compliance alerts",
    "Delivery risk items"
];

function EmptyPanel({ children, className = "" }) {
    return (
        <div className={`dashboard-empty ${className}`}>
            {children}
        </div>
    );
}

function CommandCenterPage() {
    return (
        <div className="command-center">
            <div className="page-heading-row">
                <div>
                    <h1>Command Center</h1>
                    <p>
                        Centralized operational overview of the logistics operation.
                    </p>
                </div>

                <div className="page-actions">
                    <button type="button" className="button-secondary" disabled>
                        Export
                    </button>
                    <Link to="/app/bookings/new" className="button-primary">
                        Create booking
                    </Link>
                </div>
            </div>

            <section className="metric-grid" aria-label="Operational summary">
                {metricCards.map((label) => (
                    <article className="dashboard-card metric-card" key={label}>
                        <h2>{label}</h2>
                        <div className="metric-value">—</div>
                        <p>No data available</p>
                    </article>
                ))}
            </section>

            <section className="overview-grid">
                <article className="dashboard-card shipment-overview-card">
                    <h2>Active shipment overview</h2>
                    <EmptyPanel>
                        <div className="empty-symbol" aria-hidden="true">▤</div>
                        <strong>No shipments available</strong>
                        <span>
                            Shipments will appear here once bookings are created.
                        </span>
                    </EmptyPanel>
                </article>

                <article className="dashboard-card fleet-overview-card">
                    <h2>Fleet overview</h2>
                    <EmptyPanel>
                        No vehicles available
                    </EmptyPanel>
                </article>

                <article className="dashboard-card capacity-card">
                    <h2>Capacity utilization</h2>
                    <EmptyPanel>
                        No capacity data available
                    </EmptyPanel>
                </article>
            </section>

            <section className="overview-grid lower-overview-grid">
                <article className="dashboard-card">
                    <h2>Compliance alerts</h2>
                    <EmptyPanel>No compliance alerts</EmptyPanel>
                </article>

                <article className="dashboard-card">
                    <h2>Delivery risk</h2>
                    <EmptyPanel>No delivery risk data available</EmptyPanel>
                </article>

                <article className="dashboard-card">
                    <h2>Upcoming deliveries</h2>
                    <EmptyPanel>No upcoming deliveries</EmptyPanel>
                </article>
            </section>

            <section className="bottom-overview-grid">
                <article className="dashboard-card live-map-card">
                    <h2>Live logistics map</h2>
                    <div className="map-placeholder">
                        <span>Map integration</span>
                    </div>
                </article>

                <article className="dashboard-card activities-card">
                    <h2>Recent operational activities</h2>
                    <div className="activity-table-wrap">
                        <table className="activity-table">
                            <thead>
                                <tr>
                                    <th>Time</th>
                                    <th>Activity</th>
                                    <th>Reference</th>
                                    <th>Status</th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr>
                                    <td colSpan="4" className="table-empty">
                                        No data available
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
                </article>
            </section>
        </div>
    );
}

export default CommandCenterPage;

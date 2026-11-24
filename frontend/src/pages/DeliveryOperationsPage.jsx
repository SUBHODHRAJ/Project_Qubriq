import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { deliveryOperationsService } from "../services/deliveryOperationsService";
import "../styles/dashboard.css";
import "../styles/dispatcher.css";

function DeliveryOperationsPage() {
    const [data, setData] = useState({
        totalActiveAssignments: 0,
        assigned: 0,
        pickedUp: 0,
        inTransit: 0,
        deliveries: []
    });

    const [search, setSearch] = useState("");
    const [status, setStatus] = useState("");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");

    const loadDeliveries = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const result = await deliveryOperationsService.getDeliveries({
                search,
                status
            });

            setData({
                totalActiveAssignments: result.totalActiveAssignments ?? 0,
                assigned: result.assigned ?? 0,
                pickedUp: result.pickedUp ?? 0,
                inTransit: result.inTransit ?? 0,
                deliveries: result.deliveries ?? []
            });
        } catch (err) {
            setError(err.message || "Unable to load delivery operations.");
        } finally {
            setLoading(false);
        }
    }, [search, status]);

    useEffect(() => {
        const timer = setTimeout(() => {
            loadDeliveries();
        }, 250);

        return () => clearTimeout(timer);
    }, [loadDeliveries]);

    return (
        <section className="module-page dispatcher-page">
            <div className="page-heading-row">
                <div>
                    <h1>Delivery Operations</h1>
                    <p>Monitor active driver assignments and shipment progress.</p>
                </div>

                <button
                    type="button"
                    className="button-primary"
                    onClick={loadDeliveries}
                    disabled={loading}
                >
                    {loading ? "Refreshing..." : "Refresh"}
                </button>
            </div>

            {error && (
                <div className="dispatcher-error" role="alert">
                    {error}
                </div>
            )}

            <div className="dispatcher-stats-grid">
                <article className="dashboard-card dispatcher-stat">
                    <span>Active assignments</span>
                    <strong>{data.totalActiveAssignments}</strong>
                </article>

                <article className="dashboard-card dispatcher-stat">
                    <span>Assigned</span>
                    <strong>{data.assigned}</strong>
                </article>

                <article className="dashboard-card dispatcher-stat">
                    <span>Picked up</span>
                    <strong>{data.pickedUp}</strong>
                </article>

                <article className="dashboard-card dispatcher-stat">
                    <span>In transit</span>
                    <strong>{data.inTransit}</strong>
                </article>
            </div>

            <div className="dashboard-card dispatcher-panel">
                <div className="delivery-filter-row">
                    <input
                        type="search"
                        placeholder="Search tracking, driver, vehicle or route..."
                        value={search}
                        onChange={event => setSearch(event.target.value)}
                    />

                    <select
                        value={status}
                        onChange={event => setStatus(event.target.value)}
                    >
                        <option value="">All active statuses</option>
                        <option value="Assigned">Assigned</option>
                        <option value="PickedUp">Picked up</option>
                        <option value="InTransit">In transit</option>
                    </select>
                </div>

                {loading && <p>Loading delivery assignments...</p>}

                {!loading && data.deliveries.length === 0 && (
                    <p>No active delivery assignments found.</p>
                )}

                {!loading && data.deliveries.length > 0 && (
                    <div className="dispatcher-table-wrap">
                        <table className="dispatcher-table">
                            <thead>
                                <tr>
                                    <th>Tracking</th>
                                    <th>Route</th>
                                    <th>Driver</th>
                                    <th>Vehicle</th>
                                    <th>Status</th>
                                    <th>Expected delivery</th>
                                    <th>Details</th>
                                </tr>
                            </thead>

                            <tbody>
                                {data.deliveries.map(item => (
                                    <tr key={item.id}>
                                        <td>
                                            <strong>{item.trackingNumber}</strong>
                                        </td>
                                        <td>
                                            {item.origin} → {item.destination}
                                        </td>
                                        <td>
                                            {item.driverName}
                                            {item.driverPhone && (
                                                <small className="delivery-subtext">
                                                    {item.driverPhone}
                                                </small>
                                            )}
                                        </td>
                                        <td>
                                            {item.vehicleRegistration}
                                            <small className="delivery-subtext">
                                                {item.vehicleType}
                                            </small>
                                        </td>
                                        <td>
                                            <span className="booking-status-other">
                                                {item.shipmentStatus}
                                            </span>
                                        </td>
                                        <td>
                                            {item.expectedDeliveryUtc
                                                ? new Date(item.expectedDeliveryUtc).toLocaleString()
                                                : "—"}
                                        </td>
                                        <td>
                                            <Link to={`/app/shipments/${item.shipmentId}`}>
                                                View shipment
                                            </Link>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>
        </section>
    );
}

export default DeliveryOperationsPage;

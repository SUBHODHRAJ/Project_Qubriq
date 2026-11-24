import { Link } from "react-router-dom";
import { useCallback, useEffect, useState } from "react";
import { dispatcherService } from "../services/dispatcherService";
import "../styles/dashboard.css";
import "../styles/dispatcher.css";

function formatDate(value) {
    if (!value) return "—";
    const date = new Date(value);
    return Number.isNaN(date.getTime()) ? "—" : date.toLocaleDateString();
}

function nextStatus(status) {
    if (status === "Assigned") return "PickedUp";
    if (status === "PickedUp") return "InTransit";
    if (status === "InTransit") return "Delivered";
    return null;
}

function ShipmentsPage() {
    const [items, setItems] = useState([]);
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState("All");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");
    const [busyId, setBusyId] = useState(null);

    const load = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const result = await dispatcherService.getShipments(search, status);
            setItems(Array.isArray(result) ? result : []);
        } catch (err) {
            setError(err.message || "Unable to load shipments.");
        } finally {
            setLoading(false);
        }
    }, [search, status]);

    useEffect(() => {
        load();
    }, [load]);

    async function advanceShipment(item) {
        const target = nextStatus(item.status);
        if (!target) return;

        setBusyId(item.id);
        setError("");
        setMessage("");

        try {
            await dispatcherService.updateShipmentStatus(item.id, target);
            setMessage(`${item.trackingNumber} updated to ${target}.`);
            await load();
        } catch (err) {
            setError(err.message || "Unable to update shipment.");
        } finally {
            setBusyId(null);
        }
    }

    return (
        <section className="module-page dispatcher-page">
            <div className="page-heading-row">
                <div>
                    <h1>Shipment Management</h1>
<Link to="/app/deliveries" className="delivery-operations-link">Delivery operations</Link>
                    <p>Search, monitor and update operational shipments.</p>
                </div>
            </div>

            <div className="dashboard-card dispatcher-table-card">
                <div className="dispatcher-toolbar">
                    <input
                        type="search"
                        placeholder="Search tracking number, customer or route..."
                        value={search}
                        onChange={(event) => setSearch(event.target.value)}
                    />
                    <select value={status} onChange={(event) => setStatus(event.target.value)}>
                        <option value="All">All statuses</option>
                        <option value="Draft">Draft</option>
                        <option value="Confirmed">Confirmed</option>
                        <option value="Rejected">Rejected</option>
                        <option value="Cancelled">Cancelled</option>
                        <option value="Assigned">Assigned</option>
                        <option value="PickedUp">Picked up</option>
                        <option value="InTransit">In transit</option>
                        <option value="Delivered">Delivered</option>
                    </select>
                    <button className="button-secondary" type="button" onClick={load}>
                        Refresh
                    </button>
                </div>

                {error && <div className="dispatcher-error" role="alert">{error}</div>}
                {message && <div className="dispatcher-success" role="status">{message}</div>}
                {loading && <div className="dashboard-empty">Loading shipments...</div>}

                {!loading && !error && items.length === 0 && (
                    <div className="dashboard-empty">No shipments found.</div>
                )}

                {!loading && items.length > 0 && (
                    <div className="activity-table-wrap">
                        <table className="activity-table dispatcher-table">
                            <thead>
                                <tr>
                                    <th>Tracking number</th>
                                    <th>Customer</th>
                                    <th>Route</th>
                                    <th>Cargo</th>
                                    <th>Weight</th>
                                    <th>Pickup</th>
                                    <th>Priority</th>
                                    <th>Status</th>
                                    <th>Action</th>
                                </tr>
                            </thead>
                            <tbody>
                                {items.map((item) => {
                                    const target = nextStatus(item.status);
                                    return (
                                        <tr key={item.id}>
                                            <td className="booking-reference"><Link to={`/app/shipments/${item.id}`}>{item.trackingNumber}</Link></td>
                                            <td>{item.customer}</td>
                                            <td>
                                                <div>{item.origin}</div>
                                                <small>↓ {item.destination}</small>
                                            </td>
                                            <td>{item.cargoName || item.cargoCategory || "—"}</td>
                                            <td>{item.weightKg != null ? `${item.weightKg} kg` : "—"}</td>
                                            <td>{formatDate(item.pickupDateUtc)}</td>
                                            <td>{item.priority || "—"}</td>
                                            <td><span className="booking-status-other">{item.status}</span></td>
                                            <td>
                                                {target ? (
                                                    <button
                                                        type="button"
                                                        className="button-secondary dispatcher-action"
                                                        disabled={busyId === item.id}
                                                        onClick={() => advanceShipment(item)}
                                                    >
                                                        {busyId === item.id ? "Saving..." : `Mark ${target}`}
                                                    </button>
                                                ) : "—"}
                                            </td>
                                        </tr>
                                    );
                                })}
                            </tbody>
                        </table>
                    </div>
                )}

                {!loading && items.length > 0 && (
                    <div className="booking-list-footer">Showing {items.length} shipment(s)</div>
                )}
            </div>
        </section>
    );
}

export default ShipmentsPage;



import { useCallback, useEffect, useState } from "react";
import { Link, useParams } from "react-router-dom";
import { shipmentOperationsService } from "../services/shipmentOperationsService";
import "../styles/dashboard.css";
import "../styles/dispatcher.css";

function ShipmentDetailsPage() {
    const { id } = useParams();
    const [shipment, setShipment] = useState(null);
    const [history, setHistory] = useState([]);
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [editing, setEditing] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    const [cargo, setCargo] = useState({
        cargoName: "",
        cargoCategory: "",
        packageCount: 1,
        totalWeightKg: "",
        lengthCm: "",
        widthCm: "",
        heightCm: "",
        isFragile: false,
        specialHandlingRequirements: ""
    });

    const load = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const [details, tracking] = await Promise.all([
                shipmentOperationsService.getDetails(id),
                shipmentOperationsService.getTracking(id)
            ]);

            setShipment(details);
            setHistory(tracking?.history || []);

            setCargo({
                cargoName: details.cargoName || "",
                cargoCategory: details.cargoCategory || "",
                packageCount: details.packageCount || 1,
                totalWeightKg: details.totalWeightKg ?? "",
                lengthCm: details.lengthCm ?? "",
                widthCm: details.widthCm ?? "",
                heightCm: details.heightCm ?? "",
                isFragile: details.isFragile || false,
                specialHandlingRequirements: details.specialHandlingRequirements || ""
            });
        } catch (err) {
            setError(err.message || "Unable to load shipment details.");
        } finally {
            setLoading(false);
        }
    }, [id]);

    useEffect(() => {
        load();
    }, [load]);

    function updateField(event) {
        const { name, value, type, checked } = event.target;
        setCargo(current => ({
            ...current,
            [name]: type === "checkbox" ? checked : value
        }));
    }

    async function saveCargo(event) {
        event.preventDefault();
        setSaving(true);
        setError("");
        setMessage("");

        try {
            const payload = {
                ...cargo,
                packageCount: Number(cargo.packageCount),
                totalWeightKg: Number(cargo.totalWeightKg),
                lengthCm: cargo.lengthCm === "" ? null : Number(cargo.lengthCm),
                widthCm: cargo.widthCm === "" ? null : Number(cargo.widthCm),
                heightCm: cargo.heightCm === "" ? null : Number(cargo.heightCm)
            };

            await shipmentOperationsService.updateCargo(id, payload);
            setEditing(false);
            setMessage("Cargo details saved successfully.");
            await load();
        } catch (err) {
            setError(err.message || "Unable to save cargo details.");
        } finally {
            setSaving(false);
        }
    }

    if (loading) {
        return <section className="module-page"><p>Loading shipment...</p></section>;
    }

    if (!shipment) {
        return (
            <section className="module-page">
                <div className="dispatcher-error">{error || "Shipment not found."}</div>
                <Link to="/app/shipments">Back to shipments</Link>
            </section>
        );
    }

    return (
        <section className="module-page dispatcher-page">
            <div className="page-heading-row">
                <div>
                    <Link to="/app/shipments">← Back to shipments</Link>
                    <h1>{shipment.trackingNumber}</h1>
                    <p>{shipment.origin} → {shipment.destination}</p>
                </div>
                <span className="booking-status-other">{shipment.status}</span>
            </div>

            {error && <div className="dispatcher-error" role="alert">{error}</div>}
            {message && <div className="dispatcher-success" role="status">{message}</div>}

            <div className="dispatcher-section-grid">
                <article className="dashboard-card dispatcher-panel">
                    <h2>Shipment information</h2>
                    <p>Customer: <strong>{shipment.customer}</strong></p>
                    <p>Origin: <strong>{shipment.origin}</strong></p>
                    <p>Destination: <strong>{shipment.destination}</strong></p>
                    <p>Status: <strong>{shipment.status}</strong></p>
                    <p>Priority: <strong>{shipment.priority || "—"}</strong></p>
                    <p>Pickup: <strong>{shipment.pickupDateUtc ? new Date(shipment.pickupDateUtc).toLocaleString() : "—"}</strong></p>
                    <p>Expected delivery: <strong>{shipment.expectedDeliveryUtc ? new Date(shipment.expectedDeliveryUtc).toLocaleString() : "—"}</strong></p>
                </article>

                <article className="dashboard-card dispatcher-panel">
                    <h2>Cargo summary</h2>
                    <p>Cargo: <strong>{shipment.cargoName || "—"}</strong></p>
                    <p>Category: <strong>{shipment.cargoCategory || "—"}</strong></p>
                    <p>Packages: <strong>{shipment.packageCount ?? "—"}</strong></p>
                    <p>Weight: <strong>{shipment.totalWeightKg ?? "—"} kg</strong></p>
                    <p>Dimensions: <strong>{[shipment.lengthCm, shipment.widthCm, shipment.heightCm].every(x => x != null) ? `${shipment.lengthCm} × ${shipment.widthCm} × ${shipment.heightCm} cm` : "—"}</strong></p>
                    <p>Fragile: <strong>{shipment.isFragile ? "Yes" : "No"}</strong></p>
                    <p>Special handling: <strong>{shipment.specialHandlingRequirements || "None"}</strong></p>
                    <button
                        type="button"
                        className="button-primary"
                        disabled={["Delivered", "Cancelled", "Rejected"].includes(shipment.status)}
                        onClick={() => setEditing(value => !value)}
                    >
                        {editing ? "Close editor" : "Edit cargo"}
                    </button>
                </article>
            </div>

            {editing && (
                <form className="dashboard-card fleet-form" onSubmit={saveCargo}>
                    <h2>Edit cargo details</h2>
                    <div className="fleet-form-grid">
                        <input name="cargoName" required maxLength="200" placeholder="Cargo name" value={cargo.cargoName} onChange={updateField} />
                        <input name="cargoCategory" maxLength="100" placeholder="Category" value={cargo.cargoCategory} onChange={updateField} />
                        <input name="packageCount" type="number" min="1" required placeholder="Package count" value={cargo.packageCount} onChange={updateField} />
                        <input name="totalWeightKg" type="number" min="0.01" step="0.01" required placeholder="Weight (kg)" value={cargo.totalWeightKg} onChange={updateField} />
                        <input name="lengthCm" type="number" min="0.01" step="0.01" placeholder="Length (cm)" value={cargo.lengthCm} onChange={updateField} />
                        <input name="widthCm" type="number" min="0.01" step="0.01" placeholder="Width (cm)" value={cargo.widthCm} onChange={updateField} />
                        <input name="heightCm" type="number" min="0.01" step="0.01" placeholder="Height (cm)" value={cargo.heightCm} onChange={updateField} />
                        <input name="specialHandlingRequirements" maxLength="500" placeholder="Special handling" value={cargo.specialHandlingRequirements} onChange={updateField} />
                    </div>
                    <label className="fleet-checkbox">
                        <input type="checkbox" name="isFragile" checked={cargo.isFragile} onChange={updateField} />
                        Fragile cargo
                    </label>
                    <button className="button-primary" disabled={saving}>
                        {saving ? "Saving..." : "Save cargo"}
                    </button>
                </form>
            )}

            <article className="dashboard-card dispatcher-panel">
                <h2>Tracking history</h2>
                <p className="dispatcher-updated">
                    Recorded operational audit events — not live GPS tracking.
                </p>

                {history.length === 0 && <p>No tracking audit events recorded yet.</p>}

                {history.length > 0 && (
                    <div className="tracking-timeline">
                        {history.map((event, index) => (
                            <div className="tracking-event" key={`${event.action}-${event.createdAtUtc}-${index}`}>
                                <span className="tracking-dot" />
                                <div>
                                    <strong>{event.action}</strong>
                                    <p>{event.details || "Operational event recorded."}</p>
                                    <small>{new Date(event.createdAtUtc).toLocaleString()}</small>
                                </div>
                            </div>
                        ))}
                    </div>
                )}
            </article>
        </section>
    );
}

export default ShipmentDetailsPage;

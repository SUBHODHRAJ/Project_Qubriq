import { useCallback, useEffect, useState } from "react";
import { useAuth } from "../context/AuthContext";
import { recipientService } from "../services/recipientService";
import "../styles/auth.css";

function RecipientPortalPage() {
    const { user, logout } = useAuth();
    const [shipments, setShipments] = useState([]);
    const [selected, setSelected] = useState(null);
    const [tracking, setTracking] = useState(null);
    const [loading, setLoading] = useState(true);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    const type = user?.recipientType;
    const isDriver = type === "DeliveryPersonnel";
    const isFinalRecipient = type === "FinalRecipient";

    const loadShipments = useCallback(async () => {
        setLoading(true);
        setError("");

        try {
            const result = await recipientService.getShipments();
            setShipments(Array.isArray(result) ? result : []);
        } catch (err) {
            setError(err.message || "Unable to load shipments.");
        } finally {
            setLoading(false);
        }
    }, []);

    useEffect(() => {
        loadShipments();
    }, [loadShipments]);

    async function openShipment(id) {
        setError("");
        setMessage("");
        setTracking(null);

        try {
            const result = await recipientService.getShipment(id);
            setSelected(result);
        } catch (err) {
            setError(err.message);
        }
    }

    async function loadTracking() {
        if (!selected) return;

        try {
            const result = await recipientService.getTracking(selected.id);
            setTracking(result);
        } catch (err) {
            setError(err.message);
        }
    }

    async function updateStatus(status) {
        if (!selected) return;

        setBusy(true);
        setError("");
        setMessage("");

        try {
            await recipientService.updateStatus(selected.id, status);
            setMessage(`Shipment updated to ${status}.`);
            await loadShipments();
            await openShipment(selected.id);
        } catch (err) {
            setError(err.message);
        } finally {
            setBusy(false);
        }
    }

    async function submitProof() {
        if (!selected) return;

        const proofType = window.prompt("Enter proof type (e.g. Signature, Photo, OTP):");
        if (!proofType) return;

        const remarks = window.prompt("Optional remarks:") || "";

        setBusy(true);
        setError("");
        setMessage("");

        try {
            await recipientService.submitProof(selected.id, {
                proofType,
                remarks,
                fileReference: null
            });

            setMessage("Proof metadata submitted.");
        } catch (err) {
            setError(err.message);
        } finally {
            setBusy(false);
        }
    }

    async function confirmReceipt() {
        if (!selected) return;

        if (!window.confirm("Confirm that you received this shipment?")) return;

        setBusy(true);
        setError("");
        setMessage("");

        try {
            await recipientService.confirmReceipt(selected.id);
            setMessage("Receipt confirmed successfully.");
        } catch (err) {
            setError(err.message);
        } finally {
            setBusy(false);
        }
    }

    return (
        <main className="portal-page">
            <header className="portal-header">
                <strong>Qubriq</strong>
                <div>
                    <span>{user?.fullName || "Recipient"}</span>
                    <button onClick={logout}>Sign out</button>
                </div>
            </header>

            <section className="portal-content">
                <p className="auth-eyebrow">SHIPMENT RECIPIENT PORTAL</p>
                <h1>Welcome, {user?.fullName || "Recipient"}</h1>
                <p>
                    {isDriver
                        ? "View assigned deliveries and update their progress."
                        : isFinalRecipient
                            ? "View incoming shipments and confirm received deliveries."
                            : "Your recipient account subtype is not configured."}
                </p>

                {error && <p role="alert" style={{ color: "#b42318" }}>{error}</p>}
                {message && <p role="status" style={{ color: "#067647" }}>{message}</p>}

                {!isDriver && !isFinalRecipient && (
                    <div className="portal-card">
                        Contact your administrator to configure your recipient subtype.
                    </div>
                )}

                <div className="portal-card">
                    <h2>My Shipments</h2>

                    {loading && <p>Loading shipments...</p>}

                    {!loading && shipments.length === 0 && (
                        <p>No shipments are currently linked to your account.</p>
                    )}

                    {shipments.map(shipment => (
                        <div
                            key={shipment.id}
                            style={{
                                borderBottom: "1px solid #e5e7eb",
                                padding: "14px 0",
                                display: "flex",
                                justifyContent: "space-between",
                                gap: "12px",
                                flexWrap: "wrap"
                            }}
                        >
                            <div>
                                <strong>{shipment.trackingNumber}</strong>
                                <p>{shipment.origin} → {shipment.destination}</p>
                                <small>Status: {shipment.status}</small>
                            </div>
                            <button onClick={() => openShipment(shipment.id)}>
                                View details
                            </button>
                        </div>
                    ))}
                </div>

                {selected && (
                    <div className="portal-card" style={{ marginTop: 20 }}>
                        <h2>Shipment Details</h2>
                        <p><strong>Tracking:</strong> {selected.trackingNumber}</p>
                        <p><strong>Origin:</strong> {selected.origin}</p>
                        <p><strong>Destination:</strong> {selected.destination}</p>
                        <p><strong>Status:</strong> {selected.status}</p>
                        <p><strong>Expected delivery:</strong> {selected.expectedDeliveryUtc || "Not specified"}</p>

                        <button onClick={loadTracking}>View tracking history</button>

                        {tracking && (
                            <div>
                                <h3>Tracking history</h3>
                                {tracking.history?.length
                                    ? tracking.history.map((event, index) => (
                                        <p key={index}>
                                            {event.details} — {new Date(event.createdAtUtc).toLocaleString()}
                                        </p>
                                    ))
                                    : <p>No recorded status changes yet.</p>}
                            </div>
                        )}

                        {isDriver && (
                            <div style={{ display: "flex", gap: 8, flexWrap: "wrap", marginTop: 16 }}>
                                {selected.status === "Assigned" && (
                                    <button disabled={busy} onClick={() => updateStatus("PickedUp")}>
                                        Mark picked up
                                    </button>
                                )}
                                {selected.status === "PickedUp" && (
                                    <button disabled={busy} onClick={() => updateStatus("InTransit")}>
                                        Start transit
                                    </button>
                                )}
                                {selected.status === "InTransit" && (
                                    <button disabled={busy} onClick={() => updateStatus("Delivered")}>
                                        Mark delivered
                                    </button>
                                )}
                                {selected.status === "Delivered" && (
                                    <button disabled={busy} onClick={submitProof}>
                                        Submit proof metadata
                                    </button>
                                )}
                            </div>
                        )}

                        {isFinalRecipient && selected.status === "Delivered" && (
                            <button
                                disabled={busy}
                                onClick={confirmReceipt}
                                style={{ marginTop: 16 }}
                            >
                                Confirm receipt
                            </button>
                        )}

                        <button
                            style={{ marginTop: 16, marginLeft: 8 }}
                            onClick={() => {
                                setSelected(null);
                                setTracking(null);
                            }}
                        >
                            Close
                        </button>
                    </div>
                )}
            </section>
        </main>
    );
}

export default RecipientPortalPage;

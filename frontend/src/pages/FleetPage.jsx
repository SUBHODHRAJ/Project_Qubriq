import { useCallback, useEffect, useState } from "react";
import { fleetService } from "../services/fleetService";
import "../styles/dashboard.css";
import "../styles/dispatcher.css";

function FleetPage() {
    const [vehicles, setVehicles] = useState([]);
    const [drivers, setDrivers] = useState([]);
    const [assignments, setAssignments] = useState([]);
    const [shipments, setShipments] = useState([]);
    const [tab, setTab] = useState("vehicles");
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");
    const [editingVehicleId, setEditingVehicleId] = useState(null);

    const emptyVehicleForm = {
        registrationNumber: "",
        vehicleType: "",
        capacityKg: "",
        cargoLengthCm: "",
        cargoWidthCm: "",
        cargoHeightCm: ""
    };

    const [vehicleForm, setVehicleForm] = useState(emptyVehicleForm);

    const [driverForm, setDriverForm] = useState({
        fullName: "",
        phone: "",
        licenseNumber: ""
    });

    const [assignmentForm, setAssignmentForm] = useState({
        shipmentId: "",
        driverId: "",
        vehicleId: ""
    });

    const load = useCallback(async () => {
        setLoading(true);
        setError("");

        const results = await Promise.allSettled([
            fleetService.getVehicles(),
            fleetService.getDrivers(),
            fleetService.getAssignments(),
            fleetService.getShipments()
        ]);

        if (results[0].status === "fulfilled") setVehicles(results[0].value || []);
        if (results[1].status === "fulfilled") setDrivers(results[1].value || []);
        if (results[2].status === "fulfilled") setAssignments(results[2].value || []);
        if (results[3].status === "fulfilled") setShipments(results[3].value || []);

        const failed = results.find(x => x.status === "rejected");
        if (failed) setError(failed.reason?.message || "Some fleet data could not be loaded.");

        setLoading(false);
    }, []);

    useEffect(() => { load(); }, [load]);

    const selectedShipment = shipments.find(
        x => String(x.id) === String(assignmentForm.shipmentId)
    );

    async function submitVehicle(event) {
        event.preventDefault();
        setSaving(true);
        setError("");
        setMessage("");

        try {
            const payload = {
                registrationNumber: vehicleForm.registrationNumber.trim(),
                vehicleType: vehicleForm.vehicleType.trim(),
                capacityKg: Number(vehicleForm.capacityKg),
                cargoLengthCm: Number(vehicleForm.cargoLengthCm),
                cargoWidthCm: Number(vehicleForm.cargoWidthCm),
                cargoHeightCm: Number(vehicleForm.cargoHeightCm)
            };

            if (editingVehicleId) {
                await fleetService.updateVehicle(editingVehicleId, payload);
                setMessage("Vehicle details updated successfully.");
            } else {
                await fleetService.addVehicle(payload);
                setMessage("Vehicle added successfully.");
            }

            setVehicleForm(emptyVehicleForm);
            setEditingVehicleId(null);
            await load();
        } catch (err) {
            setError(err.message || "Unable to add vehicle.");
        } finally {
            setSaving(false);
        }
    }

    function editVehicle(item) {
        setEditingVehicleId(item.id);
        setVehicleForm({
            registrationNumber: item.registrationNumber || "",
            vehicleType: item.vehicleType || "",
            capacityKg: item.capacityKg ?? "",
            cargoLengthCm: item.cargoLengthCm ?? "",
            cargoWidthCm: item.cargoWidthCm ?? "",
            cargoHeightCm: item.cargoHeightCm ?? ""
        });
        setError("");
        setMessage("");
        setTab("vehicles");
    }

    function cancelVehicleEdit() {
        setEditingVehicleId(null);
        setVehicleForm(emptyVehicleForm);
    }

    async function submitDriver(event) {
        event.preventDefault();
        setSaving(true);
        setError("");
        setMessage("");

        try {
            await fleetService.addDriver(driverForm);
            setDriverForm({ fullName: "", phone: "", licenseNumber: "" });
            setMessage("Driver added successfully.");
            await load();
        } catch (err) {
            setError(err.message || "Unable to add driver.");
        } finally {
            setSaving(false);
        }
    }

    async function submitAssignment(event) {
        event.preventDefault();
        setSaving(true);
        setError("");
        setMessage("");

        try {
            await fleetService.assign({
                shipmentId: Number(assignmentForm.shipmentId),
                driverId: Number(assignmentForm.driverId),
                vehicleId: Number(assignmentForm.vehicleId)
            });
            setAssignmentForm({ shipmentId: "", driverId: "", vehicleId: "" });
            setMessage("Shipment assigned successfully.");
            await load();
        } catch (err) {
            setError(err.message || "Unable to assign shipment.");
        } finally {
            setSaving(false);
        }
    }

    async function changeStatus(type, item, status) {
        setError("");
        setMessage("");

        try {
            if (type === "vehicle") await fleetService.updateVehicleStatus(item.id, status);
            if (type === "driver") await fleetService.updateDriverStatus(item.id, status);
            setMessage(`${type === "vehicle" ? item.registrationNumber : item.fullName} updated.`);
            await load();
        } catch (err) {
            setError(err.message || "Unable to update status.");
        }
    }

    return (
        <section className="module-page dispatcher-page">
            <div className="page-heading-row">
                <div>
                    <h1>Fleet & Capacity Planning</h1>
                    <p>Manage vehicles, drivers and shipment assignments.</p>
                </div>
                <button className="button-secondary" type="button" onClick={load}>Refresh</button>
            </div>

            <div className="dispatcher-tabs">
                {["vehicles", "drivers", "assignments"].map(item => (
                    <button
                        key={item}
                        type="button"
                        className={tab === item ? "active" : ""}
                        onClick={() => setTab(item)}
                    >
                        {item.charAt(0).toUpperCase() + item.slice(1)}
                    </button>
                ))}
            </div>

            {error && <div className="dispatcher-error" role="alert">{error}</div>}
            {message && <div className="dispatcher-success" role="status">{message}</div>}
            {loading && <div className="dashboard-empty">Loading fleet data...</div>}

            {!loading && tab === "vehicles" && (
                <>
                    <form className="dashboard-card fleet-form" onSubmit={submitVehicle}>
                        <h2>{editingVehicleId ? "Edit vehicle" : "Add vehicle"}</h2>
                        <div className="fleet-form-grid">
                            <input required maxLength="30" placeholder="Registration number" value={vehicleForm.registrationNumber} onChange={e => setVehicleForm({ ...vehicleForm, registrationNumber: e.target.value })} />
                            <input required maxLength="80" placeholder="Vehicle type" value={vehicleForm.vehicleType} onChange={e => setVehicleForm({ ...vehicleForm, vehicleType: e.target.value })} />
                            <input required type="number" min="0.01" step="0.01" placeholder="Payload capacity (kg)" value={vehicleForm.capacityKg} onChange={e => setVehicleForm({ ...vehicleForm, capacityKg: e.target.value })} />
                            <input required type="number" min="0.01" step="0.01" placeholder="Cargo length (cm)" value={vehicleForm.cargoLengthCm} onChange={e => setVehicleForm({ ...vehicleForm, cargoLengthCm: e.target.value })} />
                            <input required type="number" min="0.01" step="0.01" placeholder="Cargo width (cm)" value={vehicleForm.cargoWidthCm} onChange={e => setVehicleForm({ ...vehicleForm, cargoWidthCm: e.target.value })} />
                            <input required type="number" min="0.01" step="0.01" placeholder="Cargo height (cm)" value={vehicleForm.cargoHeightCm} onChange={e => setVehicleForm({ ...vehicleForm, cargoHeightCm: e.target.value })} />
                            <button className="button-primary" disabled={saving}>
                                {saving ? "Saving..." : editingVehicleId ? "Update vehicle" : "Add vehicle"}
                            </button>
                            {editingVehicleId && (
                                <button className="button-secondary" type="button" onClick={cancelVehicleEdit}>
                                    Cancel edit
                                </button>
                            )}
                        </div>
                    </form>

                    <div className="dashboard-card dispatcher-table-card">
                        <div className="activity-table-wrap">
                            <table className="activity-table dispatcher-table">
                                <thead><tr><th>Registration</th><th>Type</th><th>Payload</th><th>Cargo dimensions (L × W × H)</th><th>Status</th><th>Actions</th></tr></thead>
                                <tbody>
                                    {vehicles.map(item => (
                                        <tr key={item.id}>
                                            <td>{item.registrationNumber}</td>
                                            <td>{item.vehicleType}</td>
                                            <td>{item.capacityKg} kg</td>
                                            <td>
                                                {item.cargoLengthCm && item.cargoWidthCm && item.cargoHeightCm
                                                    ? `${item.cargoLengthCm} × ${item.cargoWidthCm} × ${item.cargoHeightCm} cm`
                                                    : "Dimensions not configured"}
                                            </td>
                                            <td>{item.status}</td>
                                            <td>
                                                <button className="button-secondary" type="button" onClick={() => editVehicle(item)}>
                                                    Edit
                                                </button>
                                                <select value={item.status} onChange={e => changeStatus("vehicle", item, e.target.value)}>
                                                    <option>Available</option><option>Maintenance</option><option>Inactive</option>
                                                </select>
                                            </td>
                                        </tr>
                                    ))}
                                    {vehicles.length === 0 && <tr><td colSpan="6">No vehicles registered.</td></tr>}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </>
            )}

            {!loading && tab === "drivers" && (
                <>
                    <form className="dashboard-card fleet-form" onSubmit={submitDriver}>
                        <h2>Add driver</h2>
                        <div className="fleet-form-grid">
                            <input required maxLength="150" placeholder="Full name" value={driverForm.fullName} onChange={e => setDriverForm({ ...driverForm, fullName: e.target.value })} />
                            <input maxLength="30" placeholder="Phone" value={driverForm.phone} onChange={e => setDriverForm({ ...driverForm, phone: e.target.value })} />
                            <input required maxLength="60" placeholder="License number" value={driverForm.licenseNumber} onChange={e => setDriverForm({ ...driverForm, licenseNumber: e.target.value })} />
                            <button className="button-primary" disabled={saving}>{saving ? "Saving..." : "Add driver"}</button>
                        </div>
                    </form>

                    <div className="dashboard-card dispatcher-table-card">
                        <div className="activity-table-wrap">
                            <table className="activity-table dispatcher-table">
                                <thead><tr><th>Driver</th><th>Phone</th><th>License</th><th>Status</th><th>Change status</th></tr></thead>
                                <tbody>
                                    {drivers.map(item => (
                                        <tr key={item.id}>
                                            <td>{item.fullName}</td><td>{item.phone || "—"}</td><td>{item.licenseNumber}</td><td>{item.status}</td>
                                            <td>
                                                <select value={item.status} onChange={e => changeStatus("driver", item, e.target.value)}>
                                                    <option>Available</option><option>OnLeave</option><option>Inactive</option>
                                                </select>
                                            </td>
                                        </tr>
                                    ))}
                                    {drivers.length === 0 && <tr><td colSpan="5">No drivers registered.</td></tr>}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </>
            )}

            {!loading && tab === "assignments" && (
                <>
                    <form className="dashboard-card fleet-form" onSubmit={submitAssignment}>
                        <h2>Assign shipment</h2>
                        <div className="fleet-form-grid">
                            <select required value={assignmentForm.shipmentId} onChange={e => setAssignmentForm({ ...assignmentForm, shipmentId: e.target.value })}>
                                <option value="">Select confirmed shipment</option>
                                {shipments.map(x => <option key={x.id} value={x.id}>{x.trackingNumber} — {x.customer}</option>)}
                            </select>
                            {selectedShipment && (
                                <div className="dashboard-empty">
                                    <strong>Selected cargo:</strong>{" "}
                                    {selectedShipment.cargoName || selectedShipment.cargoCategory || "Shipment cargo"}
                                    {" · "}
                                    Weight: {selectedShipment.weightKg ?? "Not recorded"} kg
                                    {" · "}
                                    Dimensions: {
                                        selectedShipment.lengthCm &&
                                        selectedShipment.widthCm &&
                                        selectedShipment.heightCm
                                            ? `${selectedShipment.lengthCm} × ${selectedShipment.widthCm} × ${selectedShipment.heightCm} cm`
                                            : "Package-level dimensions or shipment dimensions required"
                                    }
                                    {" · "}
                                    Packages: {selectedShipment.packageCount ?? "Not recorded"}
                                </div>
                            )}
                            <select required value={assignmentForm.driverId} onChange={e => setAssignmentForm({ ...assignmentForm, driverId: e.target.value })}>
                                <option value="">Select available driver</option>
                                {drivers.filter(x => x.status === "Available").map(x => <option key={x.id} value={x.id}>{x.fullName}</option>)}
                            </select>
                            <select required value={assignmentForm.vehicleId} onChange={e => setAssignmentForm({ ...assignmentForm, vehicleId: e.target.value })}>
                                <option value="">Select available vehicle</option>
                                {vehicles.filter(x => x.status === "Available").map(x => <option key={x.id} value={x.id}>{x.registrationNumber} ({x.capacityKg} kg)</option>)}
                            </select>
                            <button className="button-primary" disabled={saving || !shipments.length || !drivers.length || !vehicles.length}>{saving ? "Assigning..." : "Assign shipment"}</button>
                        </div>
                    </form>

                    <div className="dashboard-card dispatcher-table-card">
                        <div className="activity-table-wrap">
                            <table className="activity-table dispatcher-table">
                                <thead><tr><th>Tracking number</th><th>Driver</th><th>Vehicle</th><th>Assigned at</th><th>Status</th></tr></thead>
                                <tbody>
                                    {assignments.map(item => (
                                        <tr key={item.id}>
                                            <td>{item.trackingNumber}</td><td>{item.driver}</td><td>{item.vehicle}</td>
                                            <td>{new Date(item.assignedAtUtc).toLocaleString()}</td><td>{item.status}</td>
                                        </tr>
                                    ))}
                                    {assignments.length === 0 && <tr><td colSpan="5">No assignments yet.</td></tr>}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </>
            )}
        </section>
    );
}

export default FleetPage;

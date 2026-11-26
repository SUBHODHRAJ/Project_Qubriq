import { useCallback, useEffect, useMemo, useState } from "react";
import { fleetService } from "../services/fleetService";
import "../styles/dashboard.css";
import "../styles/dispatcher.css";
import "../styles/fleet-planning.css";

const EMPTY_VEHICLE = {
    registrationNumber: "", vehicleType: "", capacityKg: "",
    cargoLengthCm: "", cargoWidthCm: "", cargoHeightCm: ""
};

const EMPTY_DRIVER = { fullName: "", phone: "", licenseNumber: "" };
const EMPTY_ASSIGNMENT = { shipmentId: "", driverId: "", vehicleId: "" };

const formatNumber = value => Number(value || 0).toLocaleString(undefined, { maximumFractionDigits: 1 });
const vehicleVolume = vehicle => {
    const { cargoLengthCm: l, cargoWidthCm: w, cargoHeightCm: h } = vehicle;
    return l && w && h ? (Number(l) * Number(w) * Number(h)) / 1000000 : null;
};
const statusClass = status => String(status || "").toLowerCase().replace(/[^a-z0-9]+/g, "-");

function FleetPage() {
    const [vehicles, setVehicles] = useState([]);
    const [drivers, setDrivers] = useState([]);
    const [assignments, setAssignments] = useState([]);
    const [shipments, setShipments] = useState([]);
    const [tab, setTab] = useState("overview");
    const [loading, setLoading] = useState(true);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");
    const [editingVehicleId, setEditingVehicleId] = useState(null);
    const [vehicleForm, setVehicleForm] = useState(EMPTY_VEHICLE);
    const [driverForm, setDriverForm] = useState(EMPTY_DRIVER);
    const [assignmentForm, setAssignmentForm] = useState(EMPTY_ASSIGNMENT);
    const [vehicleSearch, setVehicleSearch] = useState("");
    const [vehicleStatus, setVehicleStatus] = useState("All");

    const load = useCallback(async () => {
        setLoading(true);
        setError("");
        const results = await Promise.allSettled([
            fleetService.getVehicles(), fleetService.getDrivers(),
            fleetService.getAssignments(), fleetService.getShipments()
        ]);
        if (results[0].status === "fulfilled") setVehicles(results[0].value || []);
        if (results[1].status === "fulfilled") setDrivers(results[1].value || []);
        if (results[2].status === "fulfilled") setAssignments(results[2].value || []);
        if (results[3].status === "fulfilled") setShipments(results[3].value || []);
        const failed = results.find(result => result.status === "rejected");
        if (failed) setError(failed.reason?.message || "Some fleet information could not be loaded.");
        setLoading(false);
    }, []);

    useEffect(() => { load(); }, [load]);

    const selectedShipment = shipments.find(item => String(item.id) === String(assignmentForm.shipmentId));
    const availableVehicles = vehicles.filter(item => item.status === "Available");
    const availableDrivers = drivers.filter(item => item.status === "Available");
    const eligibleVehicles = selectedShipment?.weightKg != null
        ? availableVehicles.filter(item => Number(item.capacityKg || 0) >= Number(selectedShipment.weightKg))
        : availableVehicles;
    const totalPayload = vehicles.reduce((sum, item) => sum + Number(item.capacityKg || 0), 0);
    const totalVolume = vehicles.reduce((sum, item) => sum + (vehicleVolume(item) || 0), 0);
    const activeAssignments = assignments.filter(item => !["Delivered", "Cancelled", "Completed"].includes(item.status));
    const filteredVehicles = useMemo(() => vehicles.filter(item => {
        const term = vehicleSearch.trim().toLowerCase();
        const matchesSearch = !term || `${item.registrationNumber} ${item.vehicleType}`.toLowerCase().includes(term);
        return matchesSearch && (vehicleStatus === "All" || item.status === vehicleStatus);
    }), [vehicles, vehicleSearch, vehicleStatus]);

    async function submitVehicle(event) {
        event.preventDefault();
        setSaving(true); setError(""); setMessage("");
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
                setMessage("Vehicle details updated.");
            } else {
                await fleetService.addVehicle(payload);
                setMessage("Vehicle added to fleet.");
            }
            setVehicleForm(EMPTY_VEHICLE); setEditingVehicleId(null);
            await load();
        } catch (err) {
            setError(err.message || "Unable to save vehicle.");
        } finally { setSaving(false); }
    }

    function editVehicle(item) {
        setEditingVehicleId(item.id);
        setVehicleForm({
            registrationNumber: item.registrationNumber || "", vehicleType: item.vehicleType || "",
            capacityKg: item.capacityKg ?? "", cargoLengthCm: item.cargoLengthCm ?? "",
            cargoWidthCm: item.cargoWidthCm ?? "", cargoHeightCm: item.cargoHeightCm ?? ""
        });
        setTab("vehicles"); setError(""); setMessage("");
    }

    function cancelVehicleEdit() {
        setEditingVehicleId(null); setVehicleForm(EMPTY_VEHICLE);
    }

    async function submitDriver(event) {
        event.preventDefault(); setSaving(true); setError(""); setMessage("");
        try {
            await fleetService.addDriver(driverForm);
            setDriverForm(EMPTY_DRIVER); setMessage("Driver added to fleet.");
            await load();
        } catch (err) { setError(err.message || "Unable to add driver."); }
        finally { setSaving(false); }
    }

    async function submitAssignment(event) {
        event.preventDefault(); setSaving(true); setError(""); setMessage("");
        try {
            await fleetService.assign({
                shipmentId: Number(assignmentForm.shipmentId),
                driverId: Number(assignmentForm.driverId),
                vehicleId: Number(assignmentForm.vehicleId)
            });
            setAssignmentForm(EMPTY_ASSIGNMENT); setMessage("Shipment assigned successfully.");
            await load();
        } catch (err) { setError(err.message || "Unable to assign shipment."); }
        finally { setSaving(false); }
    }

    async function changeStatus(type, item, status) {
        setError(""); setMessage("");
        try {
            if (type === "vehicle") await fleetService.updateVehicleStatus(item.id, status);
            else await fleetService.updateDriverStatus(item.id, status);
            setMessage(`${type === "vehicle" ? item.registrationNumber : item.fullName} status updated.`);
            await load();
        } catch (err) { setError(err.message || "Unable to update status."); }
    }

    const tabs = [
        { id: "overview", label: "Overview" },
        { id: "vehicles", label: "Vehicles", count: vehicles.length },
        { id: "drivers", label: "Drivers", count: drivers.length },
        { id: "assignments", label: "Assignments", count: assignments.length }
    ];

    return (
        <section className="module-page fleet-planning-page">
            <header className="fleet-page-header">
                <div>
                    <span className="fleet-eyebrow">OPERATIONS / RESOURCE MANAGEMENT</span>
                    <h1>Fleet & Capacity Planning</h1>
                    <p>Track vehicle availability, understand carrying capacity and allocate resources to confirmed shipments.</p>
                </div>
                <div className="fleet-header-actions">
                    <button className="button-secondary" type="button" onClick={load} disabled={loading}>
                        <span aria-hidden="true">↻</span> Refresh
                    </button>
                    <button className="button-primary" type="button" onClick={() => { setTab("vehicles"); setEditingVehicleId(null); setVehicleForm(EMPTY_VEHICLE); }}>
                        + Add vehicle
                    </button>
                </div>
            </header>

            <nav className="fleet-tabs" aria-label="Fleet sections">
                {tabs.map(item => (
                    <button key={item.id} type="button" className={tab === item.id ? "active" : ""} onClick={() => setTab(item.id)}>
                        {item.label}{item.count !== undefined && <span>{item.count}</span>}
                    </button>
                ))}
            </nav>

            {error && <div className="dispatcher-error" role="alert">{error}</div>}
            {message && <div className="dispatcher-success" role="status">{message}</div>}

            {loading ? <div className="fleet-loading">Loading fleet information…</div> : (
                <>
                    {tab === "overview" && (
                        <>
                            <div className="fleet-metric-grid">
                                <article className="fleet-metric-card">
                                    <span className="fleet-metric-icon">▣</span>
                                    <div><small>Total vehicles</small><strong>{vehicles.length}</strong><p>Registered fleet</p></div>
                                </article>
                                <article className="fleet-metric-card">
                                    <span className="fleet-metric-icon available">✓</span>
                                    <div><small>Available vehicles</small><strong>{availableVehicles.length}</strong><p>Ready for allocation</p></div>
                                </article>
                                <article className="fleet-metric-card">
                                    <span className="fleet-metric-icon">↗</span>
                                    <div><small>Active assignments</small><strong>{activeAssignments.length}</strong><p>Currently allocated</p></div>
                                </article>
                                <article className="fleet-metric-card">
                                    <span className="fleet-metric-icon">◫</span>
                                    <div><small>Registered payload</small><strong>{formatNumber(totalPayload)} <em>kg</em></strong><p>Combined rated capacity</p></div>
                                </article>
                            </div>

                            <div className="fleet-overview-grid">
                                <section className="fleet-panel">
                                    <div className="fleet-panel-heading">
                                        <div><h2>Fleet availability</h2><p>Current status of registered vehicles</p></div>
                                        <button className="fleet-text-button" onClick={() => setTab("vehicles")}>View vehicles →</button>
                                    </div>
                                    <div className="fleet-status-list">
                                        {["Available", "OnTrip", "Maintenance", "Inactive"].map(status => {
                                            const count = vehicles.filter(item => item.status === status).length;
                                            const percent = vehicles.length ? (count / vehicles.length) * 100 : 0;
                                            return <div className="fleet-status-row" key={status}>
                                                <div className="fleet-status-label"><span className={`fleet-status-dot ${statusClass(status)}`} />{status === "OnTrip" ? "On trip" : status}<strong>{count}</strong></div>
                                                <div className="fleet-progress"><span className={statusClass(status)} style={{ width: `${percent}%` }} /></div>
                                            </div>;
                                        })}
                                    </div>
                                    <div className="fleet-availability-foot"><strong>{availableVehicles.length}</strong> of {vehicles.length} vehicles are ready to be assigned</div>
                                </section>

                                <section className="fleet-panel">
                                    <div className="fleet-panel-heading">
                                        <div><h2>Capacity snapshot</h2><p>Combined registered vehicle capacity</p></div>
                                    </div>
                                    <div className="fleet-capacity-summary">
                                        <div><span>Payload capacity</span><strong>{formatNumber(totalPayload)} kg</strong></div>
                                        <div><span>Estimated cargo volume</span><strong>{formatNumber(totalVolume)} m³</strong></div>
                                    </div>
                                    <div className="fleet-capacity-note">Capacity figures describe registered vehicle specifications. Actual available space depends on existing cargo and loading constraints.</div>
                                    <button className="fleet-outline-action" onClick={() => setTab("assignments")}>Plan a shipment assignment <span>→</span></button>
                                </section>
                            </div>

                            <section className="fleet-panel">
                                <div className="fleet-panel-heading">
                                    <div><h2>Vehicles at a glance</h2><p>Quick view of fleet readiness and carrying limits</p></div>
                                    <button className="fleet-text-button" onClick={() => setTab("vehicles")}>Manage fleet →</button>
                                </div>
                                {vehicles.length ? (
                                    <div className="fleet-vehicle-grid">
                                        {vehicles.slice(0, 4).map(item => (
                                            <article className="fleet-vehicle-card" key={item.id}>
                                                <div className="fleet-vehicle-card-top"><span className="fleet-vehicle-symbol">▰</span><span className={`fleet-status-pill ${statusClass(item.status)}`}>{item.status}</span></div>
                                                <strong>{item.registrationNumber}</strong><span className="fleet-vehicle-type">{item.vehicleType}</span>
                                                <div className="fleet-vehicle-specs"><span><b>{formatNumber(item.capacityKg)}</b> kg payload</span><span><b>{vehicleVolume(item) === null ? "—" : formatNumber(vehicleVolume(item))}</b> m³ cargo space</span></div>
                                                <button className="fleet-text-button" onClick={() => editVehicle(item)}>Edit vehicle →</button>
                                            </article>
                                        ))}
                                    </div>
                                ) : <div className="fleet-empty">No vehicles have been added yet. Add your first vehicle to start planning capacity.</div>}
                            </section>
                        </>
                    )}

                    {tab === "vehicles" && (
                        <>
                            <section className="fleet-panel fleet-form-panel">
                                <div className="fleet-panel-heading">
                                    <div><h2>{editingVehicleId ? "Update vehicle details" : "Register a vehicle"}</h2><p>Enter payload limit and internal cargo dimensions to make the vehicle usable for capacity planning.</p></div>
                                    {editingVehicleId && <button className="fleet-text-button" type="button" onClick={cancelVehicleEdit}>Cancel edit</button>}
                                </div>
                                <form onSubmit={submitVehicle} className="fleet-form">
                                    <div className="fleet-form-grid">
                                        <label>Registration number<input required maxLength="30" placeholder="e.g. TN 01 AB 1234" value={vehicleForm.registrationNumber} onChange={e => setVehicleForm({ ...vehicleForm, registrationNumber: e.target.value })} /></label>
                                        <label>Vehicle type<input required maxLength="80" placeholder="e.g. 14 ft truck" value={vehicleForm.vehicleType} onChange={e => setVehicleForm({ ...vehicleForm, vehicleType: e.target.value })} /></label>
                                        <label>Maximum payload (kg)<input required type="number" min="0.01" step="0.01" placeholder="e.g. 5000" value={vehicleForm.capacityKg} onChange={e => setVehicleForm({ ...vehicleForm, capacityKg: e.target.value })} /></label>
                                        <label>Cargo length (cm)<input required type="number" min="0.01" step="0.01" placeholder="Length" value={vehicleForm.cargoLengthCm} onChange={e => setVehicleForm({ ...vehicleForm, cargoLengthCm: e.target.value })} /></label>
                                        <label>Cargo width (cm)<input required type="number" min="0.01" step="0.01" placeholder="Width" value={vehicleForm.cargoWidthCm} onChange={e => setVehicleForm({ ...vehicleForm, cargoWidthCm: e.target.value })} /></label>
                                        <label>Cargo height (cm)<input required type="number" min="0.01" step="0.01" placeholder="Height" value={vehicleForm.cargoHeightCm} onChange={e => setVehicleForm({ ...vehicleForm, cargoHeightCm: e.target.value })} /></label>
                                    </div>
                                    <div className="fleet-form-footer"><p>Internal cargo dimensions are used to estimate usable volume (m³).</p><button className="button-primary" disabled={saving}>{saving ? "Saving…" : editingVehicleId ? "Save changes" : "Add vehicle to fleet"}</button></div>
                                </form>
                            </section>

                            <section className="fleet-panel">
                                <div className="fleet-panel-heading fleet-list-heading"><div><h2>Vehicle inventory</h2><p>{vehicles.length} registered vehicle{vehicles.length === 1 ? "" : "s"}</p></div>
                                    <div className="fleet-filters"><input aria-label="Search vehicles" placeholder="Search registration or type…" value={vehicleSearch} onChange={e => setVehicleSearch(e.target.value)} /><select aria-label="Filter by status" value={vehicleStatus} onChange={e => setVehicleStatus(e.target.value)}><option>All</option><option>Available</option><option>OnTrip</option><option>Maintenance</option><option>Inactive</option></select></div>
                                </div>
                                <div className="fleet-table-wrap"><table className="fleet-table"><thead><tr><th>Vehicle</th><th>Payload limit</th><th>Cargo dimensions</th><th>Volume</th><th>Status</th><th>Actions</th></tr></thead>
                                    <tbody>{filteredVehicles.map(item => <tr key={item.id}>
                                        <td><strong>{item.registrationNumber}</strong><small>{item.vehicleType}</small></td>
                                        <td>{formatNumber(item.capacityKg)} kg</td>
                                        <td>{item.cargoLengthCm && item.cargoWidthCm && item.cargoHeightCm ? `${item.cargoLengthCm} × ${item.cargoWidthCm} × ${item.cargoHeightCm} cm` : "Not configured"}</td>
                                        <td>{vehicleVolume(item) === null ? "—" : `${formatNumber(vehicleVolume(item))} m³`}</td>
                                        <td><span className={`fleet-status-pill ${statusClass(item.status)}`}>{item.status}</span></td>
                                        <td className="fleet-row-actions"><button type="button" onClick={() => editVehicle(item)}>Edit</button><select aria-label={`Change status for ${item.registrationNumber}`} value={item.status} onChange={e => changeStatus("vehicle", item, e.target.value)}><option>Available</option><option>Maintenance</option><option>Inactive</option></select></td>
                                    </tr>)}
                                    {!filteredVehicles.length && <tr><td colSpan="6" className="fleet-empty-cell">No vehicles match this filter.</td></tr>}</tbody>
                                </table></div>
                            </section>
                        </>
                    )}

                    {tab === "drivers" && (
                        <>
                            <section className="fleet-panel fleet-form-panel">
                                <div className="fleet-panel-heading"><div><h2>Add a driver</h2><p>Register drivers and keep their availability current for shipment allocation.</p></div></div>
                                <form onSubmit={submitDriver} className="fleet-form">
                                    <div className="fleet-form-grid fleet-driver-form-grid">
                                        <label>Driver full name<input required maxLength="150" placeholder="Full name" value={driverForm.fullName} onChange={e => setDriverForm({ ...driverForm, fullName: e.target.value })} /></label>
                                        <label>Phone number<input maxLength="30" placeholder="Contact number" value={driverForm.phone} onChange={e => setDriverForm({ ...driverForm, phone: e.target.value })} /></label>
                                        <label>Driving licence number<input required maxLength="60" placeholder="Licence number" value={driverForm.licenseNumber} onChange={e => setDriverForm({ ...driverForm, licenseNumber: e.target.value })} /></label>
                                    </div>
                                    <div className="fleet-form-footer"><p>Only available drivers can be selected for a new assignment.</p><button className="button-primary" disabled={saving}>{saving ? "Saving…" : "Add driver"}</button></div>
                                </form>
                            </section>
                            <section className="fleet-panel"><div className="fleet-panel-heading"><div><h2>Driver directory</h2><p>{availableDrivers.length} available of {drivers.length} registered drivers</p></div></div>
                                <div className="fleet-table-wrap"><table className="fleet-table"><thead><tr><th>Driver</th><th>Phone</th><th>Licence</th><th>Status</th><th>Update status</th></tr></thead><tbody>
                                    {drivers.map(item => <tr key={item.id}><td><strong>{item.fullName}</strong></td><td>{item.phone || "—"}</td><td>{item.licenseNumber}</td><td><span className={`fleet-status-pill ${statusClass(item.status)}`}>{item.status}</span></td><td><select aria-label={`Change status for ${item.fullName}`} value={item.status} onChange={e => changeStatus("driver", item, e.target.value)}><option>Available</option><option>OnLeave</option><option>Inactive</option></select></td></tr>)}
                                    {!drivers.length && <tr><td colSpan="5" className="fleet-empty-cell">No drivers registered.</td></tr>}
                                </tbody></table></div>
                            </section>
                        </>
                    )}

                    {tab === "assignments" && (
                        <>
                            <section className="fleet-panel fleet-form-panel">
                                <div className="fleet-panel-heading"><div><h2>Allocate a shipment</h2><p>Choose a confirmed shipment, an available driver and a vehicle that can carry its recorded payload.</p></div></div>
                                <form onSubmit={submitAssignment} className="fleet-form">
                                    <div className="fleet-form-grid fleet-assignment-form-grid">
                                        <label>Confirmed shipment<select required value={assignmentForm.shipmentId} onChange={e => setAssignmentForm({ ...assignmentForm, shipmentId: e.target.value })}><option value="">Select shipment</option>{shipments.map(item => <option key={item.id} value={item.id}>{item.trackingNumber} — {item.customer}</option>)}</select></label>
                                        <label>Available driver<select required value={assignmentForm.driverId} onChange={e => setAssignmentForm({ ...assignmentForm, driverId: e.target.value })}><option value="">Select driver</option>{availableDrivers.map(item => <option key={item.id} value={item.id}>{item.fullName}</option>)}</select></label>
                                        <label>Available vehicle<select required value={assignmentForm.vehicleId} onChange={e => setAssignmentForm({ ...assignmentForm, vehicleId: e.target.value })}><option value="">Select vehicle</option>{eligibleVehicles.map(item => <option key={item.id} value={item.id}>{item.registrationNumber} · {item.vehicleType} · {formatNumber(item.capacityKg)} kg</option>)}</select></label>
                                    </div>
                                    {selectedShipment && <div className="fleet-shipment-summary"><strong>Shipment requirements</strong><div><span>Tracking ID<b>{selectedShipment.trackingNumber}</b></span><span>Cargo weight<b>{selectedShipment.weightKg == null ? "Not recorded" : `${formatNumber(selectedShipment.weightKg)} kg`}</b></span><span>Package count<b>{selectedShipment.packageCount ?? "Not recorded"}</b></span><span>Dimensions<b>{selectedShipment.lengthCm && selectedShipment.widthCm && selectedShipment.heightCm ? `${selectedShipment.lengthCm} × ${selectedShipment.widthCm} × ${selectedShipment.heightCm} cm` : "Not recorded"}</b></span></div></div>}
                                    <div className="fleet-form-footer"><p>Vehicle suitability should be confirmed against both payload and cargo dimensions before loading.</p><button className="button-primary" disabled={saving || !shipments.length || !availableDrivers.length || !eligibleVehicles.length}>{saving ? "Assigning…" : "Confirm assignment"}</button></div>
                                </form>
                            </section>
                            <section className="fleet-panel"><div className="fleet-panel-heading"><div><h2>Assignment history</h2><p>{assignments.length} recorded assignment{assignments.length === 1 ? "" : "s"}</p></div></div>
                                <div className="fleet-table-wrap"><table className="fleet-table"><thead><tr><th>Shipment</th><th>Driver</th><th>Vehicle</th><th>Assigned date</th><th>Status</th></tr></thead><tbody>
                                    {assignments.map(item => <tr key={item.id}><td><strong>{item.trackingNumber}</strong></td><td>{item.driver}</td><td>{item.vehicle}</td><td>{item.assignedAtUtc ? new Date(item.assignedAtUtc).toLocaleString() : "—"}</td><td><span className={`fleet-status-pill ${statusClass(item.status)}`}>{item.status}</span></td></tr>)}
                                    {!assignments.length && <tr><td colSpan="5" className="fleet-empty-cell">No shipment assignments recorded.</td></tr>}
                                </tbody></table></div>
                            </section>
                        </>
                    )}
                </>
            )}
        </section>
    );
}

export default FleetPage;

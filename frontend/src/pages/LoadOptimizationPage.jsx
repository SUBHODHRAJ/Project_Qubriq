import { useEffect, useMemo, useState } from "react";
import { loadOptimizationService } from "../services/loadOptimizationService";
import LoadScene3D from "../components/loadOptimization/LoadScene3D";
import LoadingPlanApprovalPanel from "../components/loadOptimization/LoadingPlanApprovalPanel";
import "../styles/load-optimization.css";

const number = (value, digits = 1) => {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed.toFixed(digits) : "â€”";
};

function Metric({ label, value, suffix = "" }) {
    return <div className="load-metric"><span>{label}</span><strong>{value}{suffix}</strong></div>;
}

function normalizeResult(payload) {
    const result = payload?.result ?? payload;
    return result ? { ...result, vehicleSpace: result.vehicleSpace ?? result.vehicle } : null;
}

export default function LoadOptimizationPage() {
    const [assignments, setAssignments] = useState([]);
    const [selectedKey, setSelectedKey] = useState("");
    const [runs, setRuns] = useState([]);
    const [response, setResponse] = useState(null);
    const [runId, setRunId] = useState(null);
    const [loading, setLoading] = useState(true);
    const [historyLoading, setHistoryLoading] = useState(false);
    const [running, setRunning] = useState(false);
    const [openingRunId, setOpeningRunId] = useState(null);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    const selected = useMemo(
        () => assignments.find(x => `${x.shipmentId}:${x.vehicleId}` === selectedKey) || null,
        [assignments, selectedKey]
    );

    const totalUnits = (selected?.packages || []).reduce(
        (sum, item) => sum + Number(item.quantity || 0), 0
    );

    async function refreshAssignments() {
        setLoading(true);
        setError("");
        try {
            const data = await loadOptimizationService.getAssignedShipments();
            const list = Array.isArray(data) ? data : [];
            setAssignments(list);
            if (selectedKey && !list.some(x => `${x.shipmentId}:${x.vehicleId}` === selectedKey)) {
                setSelectedKey("");
                setRuns([]);
                setResponse(null);
                setRunId(null);
            }
        } catch (err) {
            setError(err.message || "Unable to load active Fleet assignments.");
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => { refreshAssignments(); }, []); // eslint-disable-line react-hooks/exhaustive-deps

    async function chooseAssignment(key) {
        setSelectedKey(key);
        setRuns([]);
        setResponse(null);
        setRunId(null);
        setMessage("");
        setError("");
        if (!key) return;

        const item = assignments.find(x => `${x.shipmentId}:${x.vehicleId}` === key);
        if (!item) return;

        setHistoryLoading(true);
        try {
            const history = await loadOptimizationService.getShipmentRuns(item.shipmentId);
            setRuns(Array.isArray(history) ? history : []);
        } catch (err) {
            setError(err.message || "Unable to load saved optimization runs.");
        } finally {
            setHistoryLoading(false);
        }
    }

    async function runOptimization() {
        if (!selected) return;
        setRunning(true);
        setError("");
        setMessage("");
        setResponse(null);
        setRunId(null);
        try {
            const payload = await loadOptimizationService.optimizeShipment(
                selected.shipmentId, selected.vehicleId
            );
            const result = normalizeResult(payload);
            if (!result) throw new Error("The optimization API returned an empty result.");
            setResponse(result);
            setRunId(payload.optimizationRunId ?? null);
            setMessage(result.isFeasible
                ? "3D loading plan generated and saved."
                : "Partial loading plan saved. Review unplaced cargo.");

            const history = await loadOptimizationService.getShipmentRuns(selected.shipmentId);
            setRuns(Array.isArray(history) ? history : []);
        } catch (err) {
            if (err.status === 401) setError("Your session has expired. Please sign in again.");
            else if (err.status === 403) setError("You do not have permission to run cargo optimization.");
            else setError(err.message || "Unable to optimize this shipment.");
        } finally {
            setRunning(false);
        }
    }

    async function openRun(id) {
        setOpeningRunId(id);
        setError("");
        setMessage("");
        try {
            const payload = await loadOptimizationService.getRun(id);
            const result = normalizeResult(payload);
            if (!result) throw new Error("The saved optimization run is empty.");
            setResponse(result);
            setRunId(id);
            setMessage(`Loaded saved optimization run #${id}.`);
        } catch (err) {
            setError(err.message || "Unable to open saved run.");
        } finally {
            setOpeningRunId(null);
        }
    }

    const dimensions = selected
        ? `${selected.lengthCm ?? "â€”"} Ã— ${selected.widthCm ?? "â€”"} Ã— ${selected.heightCm ?? "â€”"} cm`
        : "â€”";
    const vehicleReady = selected &&
        Number(selected.lengthCm) > 0 &&
        Number(selected.widthCm) > 0 &&
        Number(selected.heightCm) > 0 &&
        Number(selected.capacityKg) > 0;

    return (
        <section className="module-page load-optimization-page">
            <div className="load-page-heading">
                <div>
                    <div className="load-eyebrow">QUBRIQ INTELLIGENCE / FLEET</div>
                    <h1>3D Cargo Optimization</h1>
                    <p>Optimize registered shipment packages using the shared loading dimensions stored in Fleet Management.</p>
                </div>
                <div className="load-engine-tag"><span />Extreme Point 3D Engine</div>
            </div>

            {error && <div className="load-alert load-alert-error" role="alert">{error}</div>}
            {message && <div className="load-alert load-alert-success" role="status">{message}</div>}

            <div className="load-section-card db-optimization-panel">
                <div className="load-section-heading">
                    <div><span className="load-step">01</span><h2>Fleet assignment</h2></div>
                    <button type="button" className="load-add-button" onClick={refreshAssignments} disabled={loading || running}>
                        {loading ? "Refreshing..." : "Refresh"}
                    </button>
                </div>
                <p className="load-helper">
                    Select an active shipment and its assigned vehicle. The optimizer reads vehicle
                    dimensions and payload from Fleet and package details from the shipment.
                </p>
                <label className="db-assignment-field">
                    Shipment / assigned vehicle
                    <select value={selectedKey} onChange={e => chooseAssignment(e.target.value)} disabled={loading || !assignments.length}>
                        <option value="">{loading ? "Loading assignments..." : "Select an active assignment"}</option>
                        {assignments.map(x => (
                            <option key={`${x.shipmentId}:${x.vehicleId}`} value={`${x.shipmentId}:${x.vehicleId}`}>
                                {x.trackingNumber} Â· {x.origin} â†’ {x.destination} Â· {x.registrationNumber}
                            </option>
                        ))}
                    </select>
                </label>

                {!loading && assignments.length === 0 && (
                    <div className="db-empty-state">
                        <strong>No active Fleet assignments</strong>
                        <span>Assign a confirmed shipment, driver and vehicle in Fleet & Capacity Planning first.</span>
                    </div>
                )}

                {selected && (
                    <>
                        <div className="db-vehicle-summary">
                            <div><span>Vehicle</span><strong>{selected.registrationNumber}</strong></div>
                            <div><span>Type</span><strong>{selected.vehicleType || "â€”"}</strong></div>
                            <div><span>Loading space (L Ã— W Ã— H)</span><strong>{dimensions}</strong></div>
                            <div><span>Maximum payload</span><strong>{number(selected.capacityKg, 2)} kg</strong></div>
                        </div>
                        {!vehicleReady && (
                            <div className="load-alert load-alert-error">
                                Vehicle loading dimensions or payload are missing. Update this vehicle in Fleet Management.
                            </div>
                        )}
                    </>
                )}
            </div>

            {selected && (
                <>
                    <div className="load-section-card">
                        <div className="load-section-heading">
                            <div><span className="load-step">02</span><h2>Registered shipment packages</h2></div>
                            <span className="load-unit-note">{selected.packages?.length || 0} groups Â· {totalUnits} units</span>
                        </div>
                        <p className="load-helper">
                            These are saved package records. Edit dimensions, quantity or fragile handling in the shipment details workflow.
                        </p>
                        <div className="load-table-wrap">
                            <table className="load-placement-table db-package-table">
                                <thead><tr><th>Package</th><th>Category</th><th>Quantity</th><th>Dimensions (L Ã— W Ã— H)</th><th>Unit weight</th><th>Handling</th></tr></thead>
                                <tbody>
                                    {(selected.packages || []).map(p => (
                                        <tr key={p.id}>
                                            <td><strong>{p.packageCode || `PKG-${p.id}`}</strong><small>{p.description}</small></td>
                                            <td>{p.category || "â€”"}</td>
                                            <td>{p.quantity}</td>
                                            <td>{p.lengthCm} Ã— {p.widthCm} Ã— {p.heightCm} cm</td>
                                            <td>{p.weightKg} kg</td>
                                            <td>{p.isFragile ? "Fragile" : "Standard"}</td>
                                        </tr>
                                    ))}
                                    {!selected.packages?.length && <tr><td colSpan="6">No package records. Add package groups before optimization.</td></tr>}
                                </tbody>
                            </table>
                        </div>
                        <div className="db-optimization-actions">
                            <button type="button" className="load-run-button" onClick={runOptimization}
                                disabled={running || !vehicleReady || !selected.packages?.length}>
                                {running ? "Calculating 3D load..." : "Run 3D cargo optimization"}
                            </button>
                        </div>
                    </div>

                    <div className="load-section-card db-history">
                        <div className="load-section-heading">
                            <div><span className="load-step">03</span><h2>Saved optimization runs</h2></div>
                            <span className="load-unit-note">{historyLoading ? "Loading..." : `${runs.length} runs`}</span>
                        </div>
                        {runs.length ? (
                            <div className="load-table-wrap">
                                <table className="load-placement-table">
                                    <thead><tr><th>Run</th><th>Created</th><th>Status</th><th>Placed</th><th>Space</th><th>Payload</th><th>Approval</th><th /></tr></thead>
                                    <tbody>
                                        {runs.map(r => (
                                            <tr key={r.id}>
                                                <td>#{r.id}</td>
                                                <td>{r.createdAtUtc ? new Date(r.createdAtUtc).toLocaleString() : "â€”"}</td>
                                                <td>{r.status}</td>
                                                <td>{r.placedPackages} / {r.totalPackages}</td>
                                                <td>{number(r.volumeUtilizationPercent)}%</td>
                                                <td>{number(r.weightUtilizationPercent ?? r.payloadUtilizationPercent)}%</td><td>{r.approvalStatus || "Pending"}</td>
                                                <td><button type="button" className="load-add-button" disabled={openingRunId !== null} onClick={() => openRun(r.id)}>{openingRunId === r.id ? "Opening..." : "View"}</button></td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        ) : <p className="load-no-items">{historyLoading ? "Retrieving saved runs..." : "No saved optimization runs for this shipment."}</p>}
                    </div>
                </>
            )}

            {response && (
                <div className="load-results">
                    <div className="load-section-heading load-results-heading">
                        <div><span className="load-step">04</span><h2>Optimization result{runId ? ` Â· Run #${runId}` : ""}</h2></div>
                        <span className={`load-result-status ${response.isFeasible ? "feasible" : "partial"}`}>
                            {response.status || (response.isFeasible ? "Feasible" : "Partial load")}
                        </span>
                    </div>
                    <div className="load-metrics-grid">
                        <Metric label="Packages placed" value={`${response.placedPackages ?? 0} / ${response.totalPackages ?? 0}`} />
                        <Metric label="Space utilization" value={number(response.volumeUtilizationPercent)} suffix="%" />
                        <Metric label="Payload utilization" value={number(response.payloadUtilizationPercent ?? response.weightUtilizationPercent)} suffix="%" />
                        <Metric label="Remaining payload" value={number(response.remainingPayloadKg)} suffix=" kg" />
                    </div>
                    <LoadingPlanApprovalPanel runId={runId} response={response} />
                    <div className="load-visual-card">
                        <div className="load-visual-heading">
                            <div><h2>Interactive 3D loading arrangement</h2><p>Drag to rotate Â· Scroll to zoom</p></div>
                            <span>{response.placements?.length || 0} placements</span>
                        </div>
                        <LoadScene3D response={response} />
                    </div>
                    <div className="load-detail-grid">
                        <div className="load-detail-card">
                            <h3>Unplaced packages</h3>
                            {response.unplacedItems?.length ? (
                                <div className="load-unplaced-list">
                                    {response.unplacedItems.map((x, i) => (
                                        <div className="load-unplaced-item" key={`${x.packageId}-${x.unitNumber}-${i}`}>
                                            <strong>{x.description || x.packageId} Â· Unit {x.unitNumber}</strong><span>{x.reason}</span>
                                        </div>
                                    ))}
                                </div>
                            ) : <p className="load-no-items">All package units were placed.</p>}
                        </div>
                        <div className="load-detail-card">
                            <h3>Optimization warnings</h3>
                            {response.warnings?.length ? <ul className="load-warning-list">{response.warnings.map((w, i) => <li key={i}>{w}</li>)}</ul> : <p className="load-no-items">No warnings reported.</p>}
                        </div>
                    </div>
                    <div className="load-detail-card load-placement-card">
                        <h3>Package placement coordinates</h3>
                        <div className="load-table-wrap">
                            <table className="load-placement-table">
                                <thead><tr><th>Package</th><th>Unit</th><th>Position X / Y / Z</th><th>Placed dimensions L / W / H</th><th>Weight</th><th>Orientation</th></tr></thead>
                                <tbody>
                                    {(response.placements || []).map((x, i) => (
                                        <tr key={`${x.packageId}-${x.unitNumber}-${i}`}>
                                            <td>{x.description || x.packageId}</td><td>{x.unitNumber}</td>
                                            <td>{x.x} / {x.y} / {x.z} cm</td>
                                            <td>{x.lengthCm} Ã— {x.widthCm} Ã— {x.heightCm} cm</td>
                                            <td>{x.weightKg} kg</td><td>{x.orientation}</td>
                                        </tr>
                                    ))}
                                    {!response.placements?.length && <tr><td colSpan="6">No packages were placed.</td></tr>}
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            )}
        </section>
    );
}



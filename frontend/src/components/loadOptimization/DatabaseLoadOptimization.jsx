import { useEffect, useMemo, useState } from "react";
import { loadOptimizationService } from "../../services/loadOptimizationService";
import LoadScene3D from "./LoadScene3D";

function DatabaseLoadOptimization() {
    const [assignments, setAssignments] = useState([]);
    const [selectedKey, setSelectedKey] = useState("");
    const [runs, setRuns] = useState([]);
    const [selectedRunId, setSelectedRunId] = useState("");
    const [response, setResponse] = useState(null);

    const [loadingAssignments, setLoadingAssignments] = useState(true);
    const [loadingRuns, setLoadingRuns] = useState(false);
    const [optimizing, setOptimizing] = useState(false);
    const [loadingRun, setLoadingRun] = useState(false);

    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    const selectedAssignment = useMemo(
        () => assignments.find(
            x => `${x.shipmentId}-${x.vehicleId}` === selectedKey
        ) || null,
        [assignments, selectedKey]
    );

    async function loadAssignments() {
        setLoadingAssignments(true);
        setError("");

        try {
            const data = await loadOptimizationService.getAssignedShipments();
            const list = Array.isArray(data) ? data : [];

            setAssignments(list);

            if (list.length > 0) {
                setSelectedKey(current => {
                    const stillExists = list.some(
                        x => `${x.shipmentId}-${x.vehicleId}` === current
                    );

                    return stillExists
                        ? current
                        : `${list[0].shipmentId}-${list[0].vehicleId}`;
                });
            } else {
                setSelectedKey("");
            }
        } catch (err) {
            setError(err.message || "Unable to load assigned shipments.");
        } finally {
            setLoadingAssignments(false);
        }
    }

    async function loadRuns(shipmentId) {
        if (!shipmentId) {
            setRuns([]);
            return;
        }

        setLoadingRuns(true);

        try {
            const data = await loadOptimizationService.getShipmentRuns(shipmentId);
            setRuns(Array.isArray(data) ? data : []);
        } catch (err) {
            setRuns([]);
            setError(err.message || "Unable to load optimization history.");
        } finally {
            setLoadingRuns(false);
        }
    }

    useEffect(() => {
        loadAssignments();
    }, []);

    useEffect(() => {
        setResponse(null);
        setSelectedRunId("");
        setRuns([]);

        if (selectedAssignment) {
            loadRuns(selectedAssignment.shipmentId);
        }
    }, [selectedKey]);

    async function runDatabaseOptimization() {
        if (!selectedAssignment) {
            setError("Select an assigned shipment first.");
            return;
        }

        if (!selectedAssignment.packages?.length) {
            setError(
                "This shipment has no registered package groups. Add package records before optimization."
            );
            return;
        }

        if (
            !selectedAssignment.lengthCm ||
            !selectedAssignment.widthCm ||
            !selectedAssignment.heightCm ||
            !selectedAssignment.capacityKg
        ) {
            setError(
                "The assigned vehicle is missing cargo dimensions or payload capacity. Update it in Fleet Management."
            );
            return;
        }

        setError("");
        setMessage("");
        setResponse(null);
        setSelectedRunId("");
        setOptimizing(true);

        try {
            const saved = await loadOptimizationService.optimizeShipment(
                selectedAssignment.shipmentId,
                selectedAssignment.vehicleId
            );

            setResponse(saved.result);
            setMessage(
                `Optimization run #${saved.optimizationRunId} saved successfully.`
            );

            await loadRuns(selectedAssignment.shipmentId);
        } catch (err) {
            setError(err.message || "Database-backed optimization failed.");
        } finally {
            setOptimizing(false);
        }
    }

    async function openSavedRun(runId) {
        if (!runId) return;

        setError("");
        setMessage("");
        setLoadingRun(true);
        setSelectedRunId(String(runId));

        try {
            const saved = await loadOptimizationService.getRun(runId);
            setResponse(saved.result);
            setMessage(`Loaded saved optimization run #${runId}.`);
        } catch (err) {
            setError(err.message || "Unable to load the saved optimization run.");
            setResponse(null);
        } finally {
            setLoadingRun(false);
        }
    }

    const packages = selectedAssignment?.packages || [];

    const totalUnits = packages.reduce(
        (sum, item) => sum + Number(item.quantity || 0),
        0
    );

    const totalWeight = packages.reduce(
        (sum, item) =>
            sum + Number(item.weightKg || 0) * Number(item.quantity || 0),
        0
    );

    return (
        <div className="db-optimization-panel">
            <div className="load-section-card">
                <div className="load-section-heading">
                    <div>
                        <span className="load-step">DB</span>
                        <h2>Optimize assigned shipment</h2>
                    </div>

                    <button
                        type="button"
                        className="load-add-button"
                        onClick={loadAssignments}
                        disabled={loadingAssignments}
                    >
                        {loadingAssignments ? "Refreshing..." : "Refresh"}
                    </button>
                </div>

                <p className="load-helper">
                    Select a shipment and its active vehicle assignment.
                    Package and vehicle data will be loaded directly from Qubriq.
                </p>

                {error && (
                    <div className="load-alert load-alert-error" role="alert">
                        {error}
                    </div>
                )}

                {message && (
                    <div className="load-alert load-alert-success" role="status">
                        {message}
                    </div>
                )}

                {loadingAssignments ? (
                    <p className="load-helper">Loading assigned shipments...</p>
                ) : assignments.length === 0 ? (
                    <p className="load-no-items">
                        No active shipment-to-vehicle assignments found.
                        Assign a shipment in Fleet Management first.
                    </p>
                ) : (
                    <>
                        <div className="load-input-grid">
                            <label>
                                Assigned shipment
                                <select
                                    value={selectedKey}
                                    onChange={e => setSelectedKey(e.target.value)}
                                >
                                    {assignments.map(item => {
                                        const key = `${item.shipmentId}-${item.vehicleId}`;

                                        return (
                                            <option key={key} value={key}>
                                                {item.trackingNumber} — {item.origin} to {item.destination}
                                            </option>
                                        );
                                    })}
                                </select>
                            </label>
                        </div>

                        {selectedAssignment && (
                            <>
                                <div className="db-vehicle-summary">
                                    <div>
                                        <span>Assigned vehicle</span>
                                        <strong>
                                            {selectedAssignment.registrationNumber}
                                        </strong>
                                    </div>

                                    <div>
                                        <span>Vehicle type</span>
                                        <strong>
                                            {selectedAssignment.vehicleType}
                                        </strong>
                                    </div>

                                    <div>
                                        <span>Cargo dimensions</span>
                                        <strong>
                                            {selectedAssignment.lengthCm ?? "—"} ×{" "}
                                            {selectedAssignment.widthCm ?? "—"} ×{" "}
                                            {selectedAssignment.heightCm ?? "—"} cm
                                        </strong>
                                    </div>

                                    <div>
                                        <span>Maximum payload</span>
                                        <strong>
                                            {selectedAssignment.capacityKg} kg
                                        </strong>
                                    </div>
                                </div>

                                <div className="db-package-summary">
                                    <div className="load-section-heading">
                                        <div>
                                            <h3>Registered shipment packages</h3>
                                        </div>

                                        <span className="load-unit-note">
                                            {packages.length} groups · {totalUnits} units
                                        </span>
                                    </div>

                                    {packages.length === 0 ? (
                                        <p className="load-no-items">
                                            No package groups are registered for this shipment.
                                        </p>
                                    ) : (
                                        <div className="load-table-wrap">
                                            <table className="load-placement-table">
                                                <thead>
                                                    <tr>
                                                        <th>Package</th>
                                                        <th>Quantity</th>
                                                        <th>Dimensions (cm)</th>
                                                        <th>Unit weight</th>
                                                        <th>Fragile</th>
                                                    </tr>
                                                </thead>

                                                <tbody>
                                                    {packages.map(item => (
                                                        <tr key={item.id}>
                                                            <td>
                                                                {item.packageCode || item.description}
                                                            </td>
                                                            <td>{item.quantity}</td>
                                                            <td>
                                                                {item.lengthCm} × {item.widthCm} × {item.heightCm}
                                                            </td>
                                                            <td>{item.weightKg} kg</td>
                                                            <td>{item.isFragile ? "Yes" : "No"}</td>
                                                        </tr>
                                                    ))}
                                                </tbody>
                                            </table>
                                        </div>
                                    )}

                                    <p className="load-helper">
                                        Total registered cargo weight: {totalWeight.toFixed(2)} kg
                                    </p>
                                </div>

                                <div className="db-optimization-actions">
                                    <button
                                        type="button"
                                        className="load-run-button"
                                        onClick={runDatabaseOptimization}
                                        disabled={
                                            optimizing ||
                                            loadingRuns ||
                                            packages.length === 0
                                        }
                                    >
                                        {optimizing
                                            ? "Optimizing and saving..."
                                            : "Run database-backed 3D optimization"}
                                    </button>
                                </div>

                                <div className="db-history">
                                    <div className="load-section-heading">
                                        <div>
                                            <h3>Saved optimization history</h3>
                                        </div>
                                        <span className="load-unit-note">
                                            {runs.length} saved runs
                                        </span>
                                    </div>

                                    {loadingRuns ? (
                                        <p className="load-helper">Loading run history...</p>
                                    ) : runs.length === 0 ? (
                                        <p className="load-no-items">
                                            No saved optimization runs for this shipment.
                                        </p>
                                    ) : (
                                        <div className="load-table-wrap">
                                            <table className="load-placement-table">
                                                <thead>
                                                    <tr>
                                                        <th>Run</th>
                                                        <th>Vehicle</th>
                                                        <th>Placed</th>
                                                        <th>Space usage</th>
                                                        <th>Created</th>
                                                        <th>Action</th>
                                                    </tr>
                                                </thead>

                                                <tbody>
                                                    {runs.map(run => (
                                                        <tr key={run.id}>
                                                            <td>#{run.id}</td>
                                                            <td>{run.vehicleRegistration}</td>
                                                            <td>
                                                                {run.placedPackages}/{run.totalPackages}
                                                            </td>
                                                            <td>
                                                                {Number(run.volumeUtilizationPercent || 0).toFixed(1)}%
                                                            </td>
                                                            <td>
                                                                {new Date(run.createdAtUtc).toLocaleString()}
                                                            </td>
                                                            <td>
                                                                <button
                                                                    type="button"
                                                                    className="load-add-button"
                                                                    onClick={() => openSavedRun(run.id)}
                                                                    disabled={loadingRun}
                                                                >
                                                                    {selectedRunId === String(run.id) && loadingRun
                                                                        ? "Loading..."
                                                                        : "View 3D"}
                                                                </button>
                                                            </td>
                                                        </tr>
                                                    ))}
                                                </tbody>
                                            </table>
                                        </div>
                                    )}
                                </div>
                            </>
                        )}
                    </>
                )}
            </div>

            {response && (
                <div className="load-results">
                    <div className="load-section-heading load-results-heading">
                        <div>
                            <span className="load-step">3D</span>
                            <h2>Database optimization result</h2>
                        </div>

                        <span className={`load-result-status ${response.isFeasible ? "feasible" : "partial"}`}>
                            {response.status}
                        </span>
                    </div>

                    <div className="load-metrics-grid">
                        <div className="load-metric">
                            <span>Packages placed</span>
                            <strong>
                                {response.placedPackages} / {response.totalPackages}
                            </strong>
                        </div>

                        <div className="load-metric">
                            <span>Space utilization</span>
                            <strong>
                                {Number(response.volumeUtilizationPercent || 0).toFixed(1)}%
                            </strong>
                        </div>

                        <div className="load-metric">
                            <span>Payload utilization</span>
                            <strong>
                                {Number(response.payloadUtilizationPercent || 0).toFixed(1)}%
                            </strong>
                        </div>

                        <div className="load-metric">
                            <span>Remaining payload</span>
                            <strong>
                                {Number(response.remainingPayloadKg || 0).toFixed(1)} kg
                            </strong>
                        </div>
                    </div>

                    <div className="load-visual-card">
                        <div className="load-visual-heading">
                            <div>
                                <h2>Interactive loading arrangement</h2>
                                <p>Saved vehicle and package coordinates.</p>
                            </div>

                            <span>{response.placements?.length || 0} placements</span>
                        </div>

                        <LoadScene3D response={response} />
                    </div>

                    <div className="load-detail-grid">
                        <div className="load-detail-card">
                            <h3>Unplaced packages</h3>

                            {response.unplacedItems?.length ? (
                                <div className="load-unplaced-list">
                                    {response.unplacedItems.map((item, index) => (
                                        <div
                                            className="load-unplaced-item"
                                            key={`${item.packageId}-${item.unitNumber}-${index}`}
                                        >
                                            <strong>
                                                Package #{item.packageId} · Unit {item.unitNumber}
                                            </strong>
                                            <span>{item.reason}</span>
                                        </div>
                                    ))}
                                </div>
                            ) : (
                                <p className="load-no-items">
                                    All package units were placed.
                                </p>
                            )}
                        </div>

                        <div className="load-detail-card">
                            <h3>Optimization warnings</h3>

                            {response.warnings?.length ? (
                                <ul className="load-warning-list">
                                    {response.warnings.map((item, index) => (
                                        <li key={index}>{item}</li>
                                    ))}
                                </ul>
                            ) : (
                                <p className="load-no-items">No warnings reported.</p>
                            )}
                        </div>
                    </div>
                </div>
            )}
        </div>
    );
}

export default DatabaseLoadOptimization;

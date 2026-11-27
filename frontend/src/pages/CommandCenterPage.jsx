import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import { dispatcherService } from "../services/dispatcherService";
import "../styles/dashboard.css";
import "../styles/dispatcher.css";

function CommandCenterPage() {
    const [data, setData] = useState(null);
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [error, setError] = useState("");

    const loadDashboard = useCallback(async (isManualRefresh = false) => {
        if (isManualRefresh) setRefreshing(true);
        else setLoading(true);
        setError("");

        try {
            const result = await dispatcherService.getDashboard();
            setData(result);
        } catch (err) {
            setError(err.message || "Dashboard could not be loaded.");
        } finally {
            setLoading(false);
            setRefreshing(false);
        }
    }, []);

    useEffect(() => {
        loadDashboard(false);
    }, [loadDashboard]);

    const totalBookings = data?.bookings?.total ?? 0;
    const pendingBookings = data?.bookings?.pending ?? 0;
    const confirmedBookings = data?.bookings?.confirmed ?? 0;
    const rejectedBookings = data?.bookings?.rejected ?? 0;

    const activeShipments = data?.shipments?.active ?? 0;
    const deliveredShipments = data?.shipments?.delivered ?? 0;
    const totalShipments = activeShipments + deliveredShipments;

    const availableVehicles = data?.fleet?.availableVehicles ?? 0;
    const availableDrivers = data?.fleet?.availableDrivers ?? 0;
    const activeAssignments = data?.fleet?.activeAssignments ?? 0;
    const totalFleetPool = availableVehicles + activeAssignments;

    const confirmationRate = totalBookings > 0
        ? Math.round((confirmedBookings / totalBookings) * 100)
        : 0;
    const fulfillmentRate = totalShipments > 0
        ? Math.round((deliveredShipments / totalShipments) * 100)
        : 0;
    const fleetUtilization = totalFleetPool > 0
        ? Math.round((activeAssignments / totalFleetPool) * 100)
        : 0;

    const kpiCards = [
        {
            label: "Total Bookings",
            value: totalBookings,
            ratio: `${confirmationRate}% confirmed`,
            detail: `${pendingBookings} awaiting review`,
            tone: pendingBookings > 0 ? "warning" : "neutral",
            to: "/app/bookings"
        },
        {
            label: "Active Shipments",
            value: activeShipments,
            ratio: `${fulfillmentRate}% delivered`,
            detail: `${deliveredShipments} completed total`,
            tone: "info",
            to: "/app/shipments"
        },
        {
            label: "Fleet Utilization",
            value: `${fleetUtilization}%`,
            ratio: `${activeAssignments} / ${totalFleetPool} deployed`,
            detail: `${availableVehicles} vehicles idle in yard`,
            tone: availableVehicles === 0 && totalFleetPool > 0 ? "danger" : "success",
            to: "/app/fleet"
        },
        {
            label: "Driver Readiness",
            value: availableDrivers,
            ratio: `${activeAssignments} on active routes`,
            detail: availableDrivers >= availableVehicles
                ? "Full vehicle-driver coverage"
                : "Driver shortfall vs. idle vehicles",
            tone: availableDrivers < availableVehicles ? "warning" : "success",
            to: "/app/fleet"
        }
    ];

    const pct = (part, whole) => (whole > 0 ? `${Math.round((part / whole) * 100)}%` : "0%");

    return (
        <section className="module-page dispatcher-page command-center">
            <header className="page-heading-row cc-header">
                <div>
                    <div className="cc-title-group">
                        <h1>Command Center</h1>
                        <span className="cc-live-pill">
                            <span className="cc-live-dot" /> Live Ops
                        </span>
                    </div>
                    <p>Real-time dispatch control, fleet utilization, and booking throughput.</p>
                </div>

                <div className="page-actions">
                    <button
                        type="button"
                        className="button-secondary cc-refresh-btn"
                        onClick={() => loadDashboard(true)}
                        disabled={loading || refreshing}
                    >
                        {refreshing ? "Syncing..." : "↻ Refresh"}
                    </button>
                    <Link to="/app/bookings" className="button-secondary">
                        Manage bookings
                    </Link>
                    <Link to="/app/shipments" className="button-primary">
                        Manage shipments
                    </Link>
                </div>
            </header>

            {error && (
                <div className="dispatcher-error cc-alert-banner" role="alert">
                    <span>{error}</span>
                    <button type="button" className="button-secondary" onClick={() => loadDashboard(true)}>
                        Retry
                    </button>
                </div>
            )}

            {loading && (
                <div className="dispatcher-metric-grid cc-kpi-grid" aria-busy="true">
                    {[1, 2, 3, 4].map((n) => (
                        <div className="dashboard-card dispatcher-metric cc-skeleton-card" key={n}>
                            <div className="cc-skeleton cc-skeleton-sm" />
                            <div className="cc-skeleton cc-skeleton-lg" />
                            <div className="cc-skeleton cc-skeleton-md" />
                        </div>
                    ))}
                </div>
            )}

            {!loading && !error && (
                <>
                    {(pendingBookings > 0 || (availableVehicles === 0 && totalFleetPool > 0)) && (
                        <div className="cc-triage-bar">
                            <div className="cc-triage-items">
                                <span className="cc-triage-badge">Action Needed</span>
                                {pendingBookings > 0 && (
                                    <span>
                                        <strong>{pendingBookings}</strong> pending booking{pendingBookings > 1 ? "s" : ""} awaiting dispatch approval
                                    </span>
                                )}
                                {availableVehicles === 0 && totalFleetPool > 0 && (
                                    <span>• <strong>0</strong> idle vehicles remaining in fleet pool</span>
                                )}
                            </div>
                            <Link to={pendingBookings > 0 ? "/app/bookings" : "/app/fleet"} className="cc-triage-link">
                                Resolve now →
                            </Link>
                        </div>
                    )}

                    <div className="dispatcher-metric-grid cc-kpi-grid">
                        {kpiCards.map((card) => (
                            <Link to={card.to} className={`dashboard-card dispatcher-metric cc-kpi-card cc-tone-${card.tone}`} key={card.label}>
                                <div className="cc-kpi-top">
                                    <span className="cc-kpi-label">{card.label}</span>
                                    <span className={`cc-kpi-chip cc-chip-${card.tone}`}>{card.ratio}</span>
                                </div>
                                <strong className="cc-kpi-value">{card.value}</strong>
                                <span className="cc-kpi-detail">{card.detail}</span>
                            </Link>
                        ))}
                    </div>

                    <div className="dispatcher-section-grid cc-panels-grid">
                        <article className="dashboard-card dispatcher-panel cc-panel">
                            <div className="cc-panel-header">
                                <div>
                                    <h2>Booking Pipeline</h2>
                                    <p className="cc-panel-sub">Distribution across {totalBookings} total requests</p>
                                </div>
                                <Link to="/app/bookings" className="cc-panel-action">Review queue →</Link>
                            </div>

                            <div className="cc-stacked-bar" role="img" aria-label="Booking status breakdown">
                                <span className="cc-bar-seg cc-seg-confirmed" style={{ width: pct(confirmedBookings, totalBookings) }} />
                                <span className="cc-bar-seg cc-seg-pending" style={{ width: pct(pendingBookings, totalBookings) }} />
                                <span className="cc-bar-seg cc-seg-rejected" style={{ width: pct(rejectedBookings, totalBookings) }} />
                            </div>

                            <div className="cc-stat-rows">
                                <div className="cc-stat-row">
                                    <span className="cc-dot cc-seg-confirmed" />
                                    <span>Confirmed</span>
                                    <strong>{confirmedBookings}</strong>
                                </div>
                                <div className="cc-stat-row">
                                    <span className="cc-dot cc-seg-pending" />
                                    <span>Pending review</span>
                                    <strong>{pendingBookings}</strong>
                                </div>
                                <div className="cc-stat-row">
                                    <span className="cc-dot cc-seg-rejected" />
                                    <span>Rejected</span>
                                    <strong>{rejectedBookings}</strong>
                                </div>
                            </div>
                        </article>

                        <article className="dashboard-card dispatcher-panel cc-panel">
                            <div className="cc-panel-header">
                                <div>
                                    <h2>Fleet & Crew Allocation</h2>
                                    <p className="cc-panel-sub">Active dispatch vs. standby capacity</p>
                                </div>
                                <Link to="/app/fleet" className="cc-panel-action">Fleet planner →</Link>
                            </div>

                            <div className="cc-stacked-bar" role="img" aria-label="Fleet utilization breakdown">
                                <span className="cc-bar-seg cc-seg-active" style={{ width: pct(activeAssignments, totalFleetPool) }} />
                                <span className="cc-bar-seg cc-seg-idle" style={{ width: pct(availableVehicles, totalFleetPool) }} />
                            </div>

                            <div className="cc-stat-rows">
                                <div className="cc-stat-row">
                                    <span className="cc-dot cc-seg-active" />
                                    <span>Active assignments</span>
                                    <strong>{activeAssignments}</strong>
                                </div>
                                <div className="cc-stat-row">
                                    <span className="cc-dot cc-seg-idle" />
                                    <span>Available vehicles</span>
                                    <strong>{availableVehicles}</strong>
                                </div>
                                <div className="cc-stat-row">
                                    <span className="cc-dot cc-seg-driver" />
                                    <span>Available drivers</span>
                                    <strong>{availableDrivers}</strong>
                                </div>
                            </div>
                        </article>
                    </div>

                    <footer className="dispatcher-updated cc-footer">
                        <span>Telemetry synced: {data?.generatedAtUtc ? new Date(data.generatedAtUtc).toLocaleString() : "—"}</span>
                        <span>Qubriq Control Tower • Stage 1</span>
                    </footer>
                </>
            )}
        </section>
    );
}

export default CommandCenterPage;
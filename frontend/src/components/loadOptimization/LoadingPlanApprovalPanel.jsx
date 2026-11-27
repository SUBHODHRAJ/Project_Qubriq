import { useEffect, useState } from "react";
import { loadOptimizationService } from "../../services/loadOptimizationService";

export default function LoadingPlanApprovalPanel({ runId, response }) {
    const [history, setHistory] = useState([]);
    const [comments, setComments] = useState("");
    const [loading, setLoading] = useState(false);
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState("");
    const [message, setMessage] = useState("");

    async function refresh() {
        if (!runId) return;
        setLoading(true);
        setError("");

        try {
            const data = await loadOptimizationService.getApprovalHistory(runId);
            setHistory(Array.isArray(data?.history) ? data.history : []);
        } catch (err) {
            setError(err.message || "Unable to retrieve approval history.");
        } finally {
            setLoading(false);
        }
    }

    useEffect(() => {
        setHistory([]);
        setComments("");
        setMessage("");
        setError("");
        refresh();
    }, [runId]);

    const latest = history[0] || null;
    const status = latest?.decision || "Pending";
    const canReview = status === "Pending";
    const canApprove = Boolean(response?.isFeasible);

    async function review(decision) {
        if (!runId || saving) return;

        setSaving(true);
        setError("");
        setMessage("");

        try {
            const result = await loadOptimizationService.reviewLoadingPlan(
                runId,
                decision,
                comments
            );

            setMessage(result.message || `Loading plan ${decision.toLowerCase()}.`);
            setComments("");
            await refresh();
        } catch (err) {
            setError(err.message || "Unable to submit the loading plan review.");
        } finally {
            setSaving(false);
        }
    }

    return (
        <section className="load-section-card approval-panel">
            <div className="load-section-heading">
                <div>
                    <span className="load-step">05</span>
                    <h2>Loading plan approval</h2>
                </div>
                <span className={`approval-status approval-${status.toLowerCase()}`}>
                    {status}
                </span>
            </div>

            <p className="load-helper">
                Review the saved 3D loading plan before dispatch. Every decision is recorded
                with the reviewer, timestamp and optional comments.
            </p>

            {loading && <p className="load-no-items">Loading review history...</p>}
            {error && <div className="load-alert load-alert-error" role="alert">{error}</div>}
            {message && <div className="load-alert load-alert-success" role="status">{message}</div>}

            {!loading && latest && (
                <div className="approval-history-item">
                    <strong>{latest.decision} by {latest.reviewerName || `User #${latest.reviewedByUserId}`}</strong>
                    <span>{latest.reviewedAtUtc ? new Date(latest.reviewedAtUtc).toLocaleString() : "Time unavailable"}</span>
                    {latest.comments && <p>{latest.comments}</p>}
                </div>
            )}

            {!loading && !latest && (
                <p className="load-no-items">This optimization run has not been reviewed yet.</p>
            )}

            {canReview && (
                <div className="approval-review-form">
                    <label htmlFor="loading-plan-comments">Review comments (optional)</label>
                    <textarea
                        id="loading-plan-comments"
                        value={comments}
                        onChange={event => setComments(event.target.value)}
                        maxLength={1000}
                        rows={3}
                        placeholder="Add review notes or reasons for rejection..."
                        disabled={saving}
                    />
                    <div className="approval-actions">
                        <button
                            type="button"
                            className="approval-reject-button"
                            onClick={() => review("Rejected")}
                            disabled={saving}
                        >
                            {saving ? "Submitting..." : "Reject plan"}
                        </button>
                        <button
                            type="button"
                            className="approval-approve-button"
                            onClick={() => review("Approved")}
                            disabled={saving}
                        >
                            {saving ? "Submitting..." : "Approve plan"}
                        </button>
                    </div>
                </div>
            )}

            {!response?.isFeasible && status === "Pending" && (
                <div className="load-alert load-alert-error">
                    This plan is partial or infeasible. It cannot be approved.
                    Generate a feasible plan or reject this run.
                </div>
            )}

            {history.length > 1 && (
                <div className="approval-history-list">
                    <h3>Review history</h3>
                    {history.slice(1).map(item => (
                        <div className="approval-history-item" key={item.id}>
                            <strong>{item.decision} by {item.reviewerName || `User #${item.reviewedByUserId}`}</strong>
                            <span>{item.reviewedAtUtc ? new Date(item.reviewedAtUtc).toLocaleString() : "Time unavailable"}</span>
                            {item.comments && <p>{item.comments}</p>}
                        </div>
                    ))}
                </div>
            )}
        </section>
    );
}



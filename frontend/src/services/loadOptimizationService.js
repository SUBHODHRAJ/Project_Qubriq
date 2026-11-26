import { apiRequest } from "./apiClient";

const BASE = "/api/load-optimization";

export const loadOptimizationService = {
    optimize: (request) =>
        apiRequest(`${BASE}/optimize`, {
            method: "POST",
            body: JSON.stringify(request)
        }),

    getAssignedShipments: () =>
        apiRequest(`${BASE}/assigned-shipments`),

    optimizeShipment: (shipmentId, vehicleId) =>
        apiRequest(
            `${BASE}/shipments/${shipmentId}/vehicles/${vehicleId}/optimize`,
            { method: "POST" }
        ),

    getShipmentRuns: (shipmentId) =>
        apiRequest(`${BASE}/shipments/${shipmentId}/runs`),

    getRun: (runId) =>
        apiRequest(`${BASE}/runs/${runId}`)
};

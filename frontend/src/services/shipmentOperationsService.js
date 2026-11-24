import { apiRequest } from "./apiClient";

const BASE = "/api/operations/shipments";

export const shipmentOperationsService = {
    getDetails: (id) => apiRequest(`${BASE}/${id}`),
    getTracking: (id) => apiRequest(`${BASE}/${id}/tracking`),
    updateCargo: (id, data) => apiRequest(`${BASE}/${id}/cargo`, {
        method: "PUT",
        body: JSON.stringify(data)
    })
};

import { apiRequest } from "./apiClient";

const BASE = "/api/fleet";

export const fleetService = {
    getVehicles: () => apiRequest(`${BASE}/vehicles`),
    addVehicle: (data) => apiRequest(`${BASE}/vehicles`, {
        method: "POST",
        body: JSON.stringify(data)
    }),
    updateVehicle: (id, data) => apiRequest(`${BASE}/vehicles/${id}`, {
        method: "PUT",
        body: JSON.stringify(data)
    }),
    updateVehicleStatus: (id, status) => apiRequest(`${BASE}/vehicles/${id}/status`, {
        method: "PATCH",
        body: JSON.stringify({ status })
    }),
    getDrivers: () => apiRequest(`${BASE}/drivers`),
    addDriver: (data) => apiRequest(`${BASE}/drivers`, {
        method: "POST",
        body: JSON.stringify(data)
    }),
    updateDriverStatus: (id, status) => apiRequest(`${BASE}/drivers/${id}/status`, {
        method: "PATCH",
        body: JSON.stringify({ status })
    }),
    getAssignments: () => apiRequest(`${BASE}/assignments`),
    assign: (data) => apiRequest(`${BASE}/assignments`, {
        method: "POST",
        body: JSON.stringify(data)
    }),
    releaseAssignment: (id) => apiRequest(`${BASE}/assignments/${id}/release`, {
        method: "POST"
    }),    getShipments: () => apiRequest("/api/dispatcher/shipments?status=Confirmed")
};



import { apiRequest } from "./apiClient";

const BASE = "/api/dispatcher";

export const dispatcherService = {
    getDashboard() {
        return apiRequest(`${BASE}/dashboard`);
    },

    getShipments(search = "", status = "All") {
        const params = new URLSearchParams();
        if (search.trim()) params.set("search", search.trim());
        if (status && status !== "All") params.set("status", status);

        const query = params.toString();
        return apiRequest(`${BASE}/shipments${query ? `?${query}` : ""}`);
    },

    updateBookingStatus(id, status) {
        return apiRequest(`${BASE}/bookings/${id}/status`, {
            method: "PATCH",
            body: JSON.stringify({ status })
        });
    },

    updateShipmentStatus(id, status) {
        return apiRequest(`${BASE}/shipments/${id}/status`, {
            method: "PATCH",
            body: JSON.stringify({ status })
        });
    }
};

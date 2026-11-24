import { apiRequest } from "./apiClient";

export const recipientService = {
    getShipments() {
        return apiRequest("/api/recipient/shipments");
    },

    getShipment(id) {
        return apiRequest(`/api/recipient/shipments/${id}`);
    },

    getTracking(id) {
        return apiRequest(`/api/recipient/shipments/${id}/tracking`);
    },

    updateStatus(id, status) {
        return apiRequest(`/api/recipient/shipments/${id}/status`, {
            method: "PATCH",
            body: JSON.stringify({ status })
        });
    },

    submitProof(id, proof) {
        return apiRequest(`/api/recipient/shipments/${id}/proof`, {
            method: "POST",
            body: JSON.stringify(proof)
        });
    },

    confirmReceipt(id) {
        return apiRequest(`/api/recipient/shipments/${id}/confirm-receipt`, {
            method: "POST"
        });
    }
};

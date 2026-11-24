import { apiRequest } from "./apiClient";

const BASE = "/api/operations/deliveries";

export const deliveryOperationsService = {
    getDeliveries: (params = {}) => {
        const query = new URLSearchParams();

        if (params.status) query.set("status", params.status);
        if (params.search) query.set("search", params.search);

        const suffix = query.toString();
        return apiRequest(suffix ? `${BASE}?${suffix}` : BASE);
    }
};

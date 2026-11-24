import { apiRequest } from "./apiClient";

const BASE_URL = "/api/customers";

export const customerService = {
    getAll(search = "", isActive = "") {
        const params = new URLSearchParams();

        if (search.trim()) {
            params.set("search", search.trim());
        }

        if (isActive !== "") {
            params.set("isActive", String(isActive));
        }

        const query = params.toString();

        return apiRequest(`${BASE_URL}${query ? `?${query}` : ""}`);
    },

    getById(id) {
        return apiRequest(`${BASE_URL}/${id}`);
    },

    create(customer) {
        return apiRequest(BASE_URL, {
            method: "POST",
            body: JSON.stringify(customer)
        });
    },

    update(id, customer) {
        return apiRequest(`${BASE_URL}/${id}`, {
            method: "PUT",
            body: JSON.stringify(customer)
        });
    },

    updateStatus(id, isActive) {
        return apiRequest(`${BASE_URL}/${id}/status`, {
            method: "PATCH",
            body: JSON.stringify({ isActive })
        });
    }
};

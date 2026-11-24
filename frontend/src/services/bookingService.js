import { apiRequest } from "./apiClient";

const BASE_URL = "/api/bookings";

export const bookingService = {
    getAll(search = "", status = "") {
        const params = new URLSearchParams();

        if (search.trim()) {
            params.set("search", search.trim());
        }

        if (status && status !== "All") {
            params.set("status", status);
        }

        const query = params.toString();

        return apiRequest(`${BASE_URL}${query ? `?${query}` : ""}`);
    },

    getCustomers() {
        return apiRequest(`${BASE_URL}/customers`);
    },

    create(booking) {
        return apiRequest(BASE_URL, {
            method: "POST",
            body: JSON.stringify(booking)
        });
    }
};

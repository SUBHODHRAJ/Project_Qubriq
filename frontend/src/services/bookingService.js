const BASE_URL = "/api/bookings";

async function request(url, options = {}) {
    const response = await fetch(url, {
        ...options,
        headers: {
            "Content-Type": "application/json",
            ...options.headers
        }
    });

    const text = await response.text();
    let data = null;

    if (text) {
        try {
            data = JSON.parse(text);
        } catch {
            data = { message: text };
        }
    }

    if (!response.ok) {
        const validationErrors = data?.errors
            ? Object.values(data.errors).flat().join(" ")
            : "";

        throw new Error(
            data?.message ||
            validationErrors ||
            data?.title ||
            `Request failed: HTTP ${response.status}`
        );
    }

    return data;
}

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

        return request(`${BASE_URL}${query ? `?${query}` : ""}`);
    },

    getCustomers() {
        return request(`${BASE_URL}/customers`);
    },

    create(booking) {
        return request(BASE_URL, {
            method: "POST",
            body: JSON.stringify(booking)
        });
    }
};
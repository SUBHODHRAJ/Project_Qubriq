const BASE_URL = "/api/customers";

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
        throw new Error(
            data?.message ||
            data?.title ||
            `Request failed: HTTP ${response.status}`
        );
    }

    return data;
}

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

        return request(`${BASE_URL}${query ? `?${query}` : ""}`);
    },

    getById(id) {
        return request(`${BASE_URL}/${id}`);
    },

    create(customer) {
        return request(BASE_URL, {
            method: "POST",
            body: JSON.stringify(customer)
        });
    },

    update(id, customer) {
        return request(`${BASE_URL}/${id}`, {
            method: "PUT",
            body: JSON.stringify(customer)
        });
    },

    updateStatus(id, isActive) {
        return request(`${BASE_URL}/${id}/status`, {
            method: "PATCH",
            body: JSON.stringify({ isActive })
        });
    }
};

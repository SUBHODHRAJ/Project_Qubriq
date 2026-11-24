const TOKEN_KEY = "qubriq_access_token";

export function getAccessToken() {
    return sessionStorage.getItem(TOKEN_KEY);
}

export function setAccessToken(token) {
    if (token) {
        sessionStorage.setItem(TOKEN_KEY, token);
    } else {
        sessionStorage.removeItem(TOKEN_KEY);
    }
}

export async function apiRequest(url, options = {}) {
    const headers = {
        ...options.headers
    };

    if (options.body && !headers["Content-Type"]) {
        headers["Content-Type"] = "application/json";
    }

    const token = getAccessToken();

    if (token) {
        headers.Authorization = `Bearer ${token}`;
    }

    const response = await fetch(url, {
        ...options,
        headers
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

        const error = new Error(
            data?.message ||
            validationErrors ||
            data?.title ||
            `Request failed: HTTP ${response.status}`
        );

        error.status = response.status;
        error.data = data;
        throw error;
    }

    return data;
}

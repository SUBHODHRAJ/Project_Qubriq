import { apiRequest, setAccessToken } from "./apiClient";

export const authService = {
    async login(email, password) {
        const result = await apiRequest("/api/auth/login", {
            method: "POST",
            body: JSON.stringify({ email, password })
        });

        if (!result?.accessToken || !result?.user) {
            throw new Error("Invalid login response from server.");
        }

        setAccessToken(result.accessToken);

        sessionStorage.setItem(
            "qubriq_user",
            JSON.stringify(result.user)
        );

        if (result.expiresAtUtc) {
            sessionStorage.setItem(
                "qubriq_expires_at",
                result.expiresAtUtc
            );
        }

        return result;
    },

    logout() {
        setAccessToken(null);
        sessionStorage.removeItem("qubriq_user");
        sessionStorage.removeItem("qubriq_expires_at");
    }
};

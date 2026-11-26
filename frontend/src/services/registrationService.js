import { apiRequest } from "./apiClient";
export const registrationService = {
    publicPolicy: () => apiRequest("/api/auth/registration-policy"),
    register: payload => apiRequest("/api/auth/register", { method: "POST", body: JSON.stringify(payload) }),
    adminPolicy: () => apiRequest("/api/admin/settings/registration"),
    savePolicy: payload => apiRequest("/api/admin/settings/registration", { method: "PUT", body: JSON.stringify(payload) }),
    pending: () => apiRequest("/api/admin/users/pending"),
    approve: id => apiRequest(`/api/admin/users/${id}/approve`, { method: "POST" })
};

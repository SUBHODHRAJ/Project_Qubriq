import { apiRequest } from "./apiClient";

export const cargoService = {
  async getShipments() {
    const result = await apiRequest("/api/operations/shipments");
    return Array.isArray(result) ? result : result?.data ?? [];
  },

  async getPackages(shipmentId) {
    const result = await apiRequest(
      `/api/shipments/${shipmentId}/packages`
    );
    return Array.isArray(result) ? result : result?.data ?? [];
  },

  async addPackage(shipmentId, payload) {
    return apiRequest(`/api/shipments/${shipmentId}/packages`, {
      method: "POST",
      body: JSON.stringify(payload),
    });
  },

  async updatePackage(shipmentId, packageId, payload) {
    return apiRequest(
      `/api/shipments/${shipmentId}/packages/${packageId}`,
      {
        method: "PUT",
        body: JSON.stringify(payload),
      }
    );
  },

  async deletePackage(shipmentId, packageId) {
    return apiRequest(
      `/api/shipments/${shipmentId}/packages/${packageId}`,
      { method: "DELETE" }
    );
  },
};

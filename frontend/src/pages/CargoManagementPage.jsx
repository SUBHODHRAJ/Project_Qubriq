
import { useEffect, useMemo, useState } from "react";
import { cargoService } from "../services/cargoService";
import "../styles/cargo-management.css";

const emptyForm = {
  description: "",
  packageCode: "",
  category: "",
  quantity: 1,
  lengthCm: "",
  widthCm: "",
  heightCm: "",
  weightKg: "",
  isFragile: false,
  specialHandlingRequirements: "",
};

function getErrorMessage(error) {
  return error?.message || "Something went wrong. Please try again.";
}

export default function CargoManagementPage() {
  const [shipments, setShipments] = useState([]);
  const [selectedShipmentId, setSelectedShipmentId] = useState("");
  const [packages, setPackages] = useState([]);
  const [search, setSearch] = useState("");

  const [loadingShipments, setLoadingShipments] = useState(true);
  const [loadingPackages, setLoadingPackages] = useState(false);
  const [saving, setSaving] = useState(false);

  const [modalOpen, setModalOpen] = useState(false);
  const [editingPackage, setEditingPackage] = useState(null);
  const [form, setForm] = useState(emptyForm);

  const [error, setError] = useState("");
  const [success, setSuccess] = useState("");

  async function loadShipments() {
    try {
      setLoadingShipments(true);
      setError("");

      const data = await cargoService.getShipments();
      setShipments(data);

      if (data.length > 0) {
        setSelectedShipmentId((current) =>
          current && data.some(
            (item) => String(item.id) === String(current)
          )
            ? current
            : String(data[0].id)
        );
      } else {
        setSelectedShipmentId("");
        setPackages([]);
      }
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setLoadingShipments(false);
    }
  }

  async function loadPackages(shipmentId) {
    if (!shipmentId) {
      setPackages([]);
      return;
    }

    try {
      setLoadingPackages(true);
      setError("");

      const data = await cargoService.getPackages(shipmentId);
      setPackages(data);
    } catch (err) {
      setPackages([]);
      setError(getErrorMessage(err));
    } finally {
      setLoadingPackages(false);
    }
  }

  useEffect(() => {
    loadShipments();
  }, []);

  useEffect(() => {
    loadPackages(selectedShipmentId);
  }, [selectedShipmentId]);

  const selectedShipment = shipments.find(
    (shipment) =>
      String(shipment.id) === String(selectedShipmentId)
  );

  const filteredPackages = useMemo(() => {
    const term = search.trim().toLowerCase();

    if (!term) return packages;

    return packages.filter((item) =>
      [
        item.description,
        item.packageCode,
        item.category,
        item.specialHandlingRequirements,
      ]
        .filter(Boolean)
        .some((value) =>
          String(value).toLowerCase().includes(term)
        )
    );
  }, [packages, search]);

  const totalQuantity = packages.reduce(
    (sum, item) => sum + Number(item.quantity || 0),
    0
  );

  const totalWeight = packages.reduce(
    (sum, item) =>
      sum +
      Number(item.weightKg || 0) *
        Number(item.quantity || 0),
    0
  );

  const totalVolume = packages.reduce(
    (sum, item) =>
      sum +
      (
        Number(item.lengthCm || 0) *
        Number(item.widthCm || 0) *
        Number(item.heightCm || 0) *
        Number(item.quantity || 0)
      ) / 1000000,
    0
  );

  function openAddModal() {
    setEditingPackage(null);
    setForm(emptyForm);
    setError("");
    setModalOpen(true);
  }

  function openEditModal(item) {
    setEditingPackage(item);

    setForm({
      description: item.description ?? "",
      packageCode: item.packageCode ?? "",
      category: item.category ?? "",
      quantity: item.quantity ?? 1,
      lengthCm: item.lengthCm ?? "",
      widthCm: item.widthCm ?? "",
      heightCm: item.heightCm ?? "",
      weightKg: item.weightKg ?? "",
      isFragile: Boolean(item.isFragile),
      specialHandlingRequirements:
        item.specialHandlingRequirements ?? "",
    });

    setError("");
    setModalOpen(true);
  }

  function updateField(event) {
    const { name, value, type, checked } = event.target;

    setForm((current) => ({
      ...current,
      [name]: type === "checkbox" ? checked : value,
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();

    if (!selectedShipmentId) {
      setError("Select a shipment first.");
      return;
    }

    const payload = {
      description: form.description.trim(),
      packageCode: form.packageCode.trim(),
      category: form.category.trim(),
      quantity: Number(form.quantity),
      lengthCm: Number(form.lengthCm),
      widthCm: Number(form.widthCm),
      heightCm: Number(form.heightCm),
      weightKg: Number(form.weightKg),
      isFragile: form.isFragile,
      specialHandlingRequirements:
        form.specialHandlingRequirements.trim(),
    };

    if (
      !payload.description ||
      !payload.packageCode ||
      !payload.category
    ) {
      setError(
        "Description, package code and category are required."
      );
      return;
    }

    if (
      !Number.isInteger(payload.quantity) ||
      payload.quantity < 1 ||
      payload.lengthCm <= 0 ||
      payload.widthCm <= 0 ||
      payload.heightCm <= 0 ||
      payload.weightKg <= 0
    ) {
      setError(
        "Enter a valid quantity, dimensions and weight greater than zero."
      );
      return;
    }

    try {
      setSaving(true);
      setError("");
      setSuccess("");

      if (editingPackage) {
        await cargoService.updatePackage(
          selectedShipmentId,
          editingPackage.id,
          payload
        );

        setSuccess("Package updated successfully.");
      } else {
        await cargoService.addPackage(
          selectedShipmentId,
          payload
        );

        setSuccess("Package added successfully.");
      }

      setModalOpen(false);
      await loadPackages(selectedShipmentId);
    } catch (err) {
      setError(getErrorMessage(err));
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(item) {
    const confirmed = window.confirm(
      `Delete package "${item.packageCode}"?`
    );

    if (!confirmed) return;

    try {
      setError("");
      setSuccess("");

      await cargoService.deletePackage(
        selectedShipmentId,
        item.id
      );

      setSuccess("Package deleted successfully.");
      await loadPackages(selectedShipmentId);
    } catch (err) {
      setError(getErrorMessage(err));
    }
  }

  return (
    <main className="cargo-page">
      <div className="cargo-heading">
        <div>
          <span className="cargo-eyebrow">
            OPERATIONS / INVENTORY
          </span>

          <h1>Cargo Management</h1>

          <p>
            Manage shipment packages, dimensions and handling
            requirements.
          </p>
        </div>

        <button
          className="cargo-primary-button"
          onClick={openAddModal}
          disabled={!selectedShipmentId}
        >
          + Add Package
        </button>
      </div>

      {error && (
        <div className="cargo-alert cargo-alert-error">
          {error}
        </div>
      )}

      {success && (
        <div className="cargo-alert cargo-alert-success">
          {success}
        </div>
      )}

      <section className="cargo-toolbar">
        <label htmlFor="cargo-shipment">
          Select shipment
        </label>

        <select
          id="cargo-shipment"
          value={selectedShipmentId}
          onChange={(event) => {
            setSelectedShipmentId(event.target.value);
            setSuccess("");
          }}
          disabled={loadingShipments}
        >
          {loadingShipments && (
            <option value="">Loading shipments...</option>
          )}

          {!loadingShipments && shipments.length === 0 && (
            <option value="">No shipments available</option>
          )}

          {shipments.map((shipment) => (
            <option key={shipment.id} value={shipment.id}>
              {shipment.trackingNumber ||
                `Shipment #${shipment.id}`}
              {shipment.origin && shipment.destination
                ? `  -  ${shipment.origin}  ->  ${shipment.destination}`
                : ""}
            </option>
          ))}
        </select>

        <input
          type="search"
          placeholder="Search packages..."
          value={search}
          onChange={(event) => setSearch(event.target.value)}
          aria-label="Search packages"
        />
      </section>

      <section className="cargo-summary">
        <article>
          <span>Package records</span>
          <strong>{packages.length}</strong>
        </article>

        <article>
          <span>Total quantity</span>
          <strong>{totalQuantity}</strong>
        </article>

        <article>
          <span>Total weight</span>
          <strong>{totalWeight.toFixed(2)} kg</strong>
        </article>

        <article>
          <span>Total volume</span>
          <strong>{totalVolume.toFixed(3)} m3</strong>
        </article>
      </section>

      <section className="cargo-table-card">
        <div className="cargo-table-heading">
          <div>
            <h2>Shipment packages</h2>

            <p>
              {selectedShipment?.trackingNumber ||
                "Select a shipment"}
              {selectedShipment?.status
                ? `  |  ${selectedShipment.status}`
                : ""}
            </p>
          </div>

          <span>{filteredPackages.length} records</span>
        </div>

        {loadingPackages ? (
          <div className="cargo-empty">
            Loading packages...
          </div>
        ) : filteredPackages.length === 0 ? (
          <div className="cargo-empty">
            <strong>No packages found</strong>
            <p>
              Add a package to this shipment to get started.
            </p>
          </div>
        ) : (
          <div className="cargo-table-scroll">
            <table className="cargo-table">
              <thead>
                <tr>
                  <th>Package</th>
                  <th>Category</th>
                  <th>Quantity</th>
                  <th>Dimensions (cm)</th>
                  <th>Weight (kg)</th>
                  <th>Handling</th>
                  <th>Actions</th>
                </tr>
              </thead>

              <tbody>
                {filteredPackages.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>{item.description}</strong>
                      <small>{item.packageCode}</small>
                    </td>

                    <td>{item.category}</td>
                    <td>{item.quantity}</td>

                    <td>
                      {item.lengthCm}  x  {item.widthCm}  x {" "}
                      {item.heightCm}
                    </td>

                    <td>
                      {Number(item.weightKg).toFixed(2)}
                    </td>

                    <td>
                      {item.isFragile && (
                        <span className="cargo-tag cargo-tag-fragile">
                          Fragile
                        </span>
                      )}

                      {item.specialHandlingRequirements && (
                        <span className="cargo-tag">
                          Special handling
                        </span>
                      )}

                      {!item.isFragile &&
                        !item.specialHandlingRequirements &&
                        "Standard"}
                    </td>

                    <td>
                      <div className="cargo-actions">
                        <button
                          onClick={() => openEditModal(item)}
                        >
                          Edit
                        </button>

                        <button
                          className="cargo-delete-button"
                          onClick={() => handleDelete(item)}
                        >
                          Delete
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      {modalOpen && (
        <div
          className="cargo-modal-backdrop"
          onMouseDown={(event) => {
            if (event.target === event.currentTarget) {
              setModalOpen(false);
            }
          }}
        >
          <section
            className="cargo-modal"
            role="dialog"
            aria-modal="true"
            aria-labelledby="cargo-modal-title"
          >
            <div className="cargo-modal-heading">
              <div>
                <span className="cargo-eyebrow">
                  PACKAGE DETAILS
                </span>

                <h2 id="cargo-modal-title">
                  {editingPackage
                    ? "Edit package"
                    : "Add package"}
                </h2>
              </div>

              <button
                type="button"
                className="cargo-close-button"
                onClick={() => setModalOpen(false)}
                aria-label="Close"
              >
                 x 
              </button>
            </div>

            <form onSubmit={handleSubmit}>
              <div className="cargo-form-grid">
                <label>
                  Description *
                  <input
                    name="description"
                    value={form.description}
                    onChange={updateField}
                    required
                    maxLength={250}
                  />
                </label>

                <label>
                  Package code *
                  <input
                    name="packageCode"
                    value={form.packageCode}
                    onChange={updateField}
                    required
                    maxLength={80}
                  />
                </label>

                <label>
                  Category *
                  <input
                    name="category"
                    value={form.category}
                    onChange={updateField}
                    required
                    maxLength={80}
                    placeholder="Electronics, Documents..."
                  />
                </label>

                <label>
                  Quantity *
                  <input
                    type="number"
                    name="quantity"
                    min="1"
                    step="1"
                    value={form.quantity}
                    onChange={updateField}
                    required
                  />
                </label>

                <label>
                  Length (cm) *
                  <input
                    type="number"
                    name="lengthCm"
                    min="0.01"
                    step="0.01"
                    value={form.lengthCm}
                    onChange={updateField}
                    required
                  />
                </label>

                <label>
                  Width (cm) *
                  <input
                    type="number"
                    name="widthCm"
                    min="0.01"
                    step="0.01"
                    value={form.widthCm}
                    onChange={updateField}
                    required
                  />
                </label>

                <label>
                  Height (cm) *
                  <input
                    type="number"
                    name="heightCm"
                    min="0.01"
                    step="0.01"
                    value={form.heightCm}
                    onChange={updateField}
                    required
                  />
                </label>

                <label>
                  Unit weight (kg) *
                  <input
                    type="number"
                    name="weightKg"
                    min="0.01"
                    step="0.01"
                    value={form.weightKg}
                    onChange={updateField}
                    required
                  />
                </label>

                <label className="cargo-full-width">
                  Special handling requirements
                  <textarea
                    name="specialHandlingRequirements"
                    value={form.specialHandlingRequirements}
                    onChange={updateField}
                    rows="3"
                    maxLength={500}
                    placeholder="Temperature control, keep upright..."
                  />
                </label>

                <label className="cargo-checkbox-label cargo-full-width">
                  <input
                    type="checkbox"
                    name="isFragile"
                    checked={form.isFragile}
                    onChange={updateField}
                  />
                  This package is fragile
                </label>
              </div>

              {error && (
                <div className="cargo-alert cargo-alert-error">
                  {error}
                </div>
              )}

              <div className="cargo-modal-actions">
                <button
                  type="button"
                  className="cargo-secondary-button"
                  onClick={() => setModalOpen(false)}
                  disabled={saving}
                >
                  Cancel
                </button>

                <button
                  type="submit"
                  className="cargo-primary-button"
                  disabled={saving}
                >
                  {saving
                    ? "Saving..."
                    : editingPackage
                      ? "Save changes"
                      : "Add package"}
                </button>
              </div>
            </form>
          </section>
        </div>
      )}
    </main>
  );
}





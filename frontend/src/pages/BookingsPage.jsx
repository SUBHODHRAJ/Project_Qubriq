import { useEffect, useState } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { bookingService } from "../services/bookingService";
import "../styles/bookings.css";
import "../styles/dashboard.css";

const emptyForm = {
    customerId: "",
    origin: "",
    destination: "",
    cargoName: "",
    cargoCategory: "",
    packageCount: "1",
    totalWeightKg: "",
    lengthCm: "",
    widthCm: "",
    heightCm: "",
    isFragile: false,
    specialHandlingRequirements: "",
    priority: "Normal",
    pickupDate: "",
    expectedDeliveryDate: "",
    deliveryTimeWindow: "",
    additionalInstructions: "",
    status: "Confirmed"
};

function formatDate(value) {
    if (!value) return "—";

    const date = new Date(value);

    if (Number.isNaN(date.getTime())) return "—";

    return date.toLocaleDateString();
}

function statusClass(status) {
    const normalized = (status || "").toLowerCase();

    if (normalized === "confirmed") return "booking-status-confirmed";
    if (normalized === "draft") return "booking-status-draft";

    return "booking-status-other";
}

function BookingsPage() {
    const location = useLocation();
    const navigate = useNavigate();

    const [bookings, setBookings] = useState([]);
    const [customers, setCustomers] = useState([]);
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState("All");
    const [loading, setLoading] = useState(true);
    const [customersLoading, setCustomersLoading] = useState(false);
    const [error, setError] = useState("");
    const [customerError, setCustomerError] = useState("");
    const [showForm, setShowForm] = useState(
        location.pathname.endsWith("/new")
    );
    const [form, setForm] = useState(emptyForm);
    const [saving, setSaving] = useState(false);
    const [formError, setFormError] = useState("");
    const [success, setSuccess] = useState("");
    const [reloadToken, setReloadToken] = useState(0);

    useEffect(() => {
        setShowForm(location.pathname.endsWith("/new"));
    }, [location.pathname]);

    useEffect(() => {
        let cancelled = false;

        async function loadBookings() {
            setLoading(true);
            setError("");

            try {
                const result = await bookingService.getAll(search, status);

                if (!cancelled) {
                    setBookings(Array.isArray(result) ? result : []);
                }
            } catch (err) {
                if (!cancelled) {
                    setBookings([]);
                    setError(err.message || "Unable to load bookings.");
                }
            } finally {
                if (!cancelled) setLoading(false);
            }
        }

        loadBookings();

        return () => {
            cancelled = true;
        };
    }, [search, status, reloadToken]);

    useEffect(() => {
        if (!showForm) return;

        let cancelled = false;

        async function loadCustomers() {
            setCustomersLoading(true);
            setCustomerError("");

            try {
                const result = await bookingService.getCustomers();

                if (!cancelled) {
                    setCustomers(Array.isArray(result) ? result : []);
                }
            } catch (err) {
                if (!cancelled) {
                    setCustomers([]);
                    setCustomerError(
                        err.message || "Unable to load active customers."
                    );
                }
            } finally {
                if (!cancelled) setCustomersLoading(false);
            }
        }

        loadCustomers();

        return () => {
            cancelled = true;
        };
    }, [showForm]);

    function openCreateForm() {
        setForm(emptyForm);
        setFormError("");
        setSuccess("");
        navigate("/app/bookings/new");
    }

    function closeCreateForm() {
        if (saving) return;

        setShowForm(false);
        setForm(emptyForm);
        setFormError("");
        navigate("/app/bookings");
    }

    function handleInputChange(event) {
        const { name, value, type, checked } = event.target;

        setForm((current) => ({
            ...current,
            [name]: type === "checkbox" ? checked : value
        }));
    }

    async function handleSubmit(event) {
        event.preventDefault();
        setFormError("");
        setSuccess("");

        if (!form.customerId) {
            setFormError("Please select an active customer.");
            return;
        }

        if (!form.pickupDate) {
            setFormError("Please select a pickup date.");
            return;
        }

        if (
            form.expectedDeliveryDate &&
            form.expectedDeliveryDate < form.pickupDate
        ) {
            setFormError(
                "Expected delivery date cannot be earlier than pickup date."
            );
            return;
        }

        const toUtc = (dateValue) => {
            if (!dateValue) return null;

            const date = new Date(`${dateValue}T00:00:00`);

            return Number.isNaN(date.getTime())
                ? null
                : date.toISOString();
        };

        const payload = {
            customerId: Number(form.customerId),
            origin: form.origin.trim(),
            destination: form.destination.trim(),
            cargoName: form.cargoName.trim(),
            cargoCategory: form.cargoCategory.trim() || null,
            packageCount: Number(form.packageCount),
            totalWeightKg: Number(form.totalWeightKg),
            lengthCm: form.lengthCm ? Number(form.lengthCm) : null,
            widthCm: form.widthCm ? Number(form.widthCm) : null,
            heightCm: form.heightCm ? Number(form.heightCm) : null,
            isFragile: form.isFragile,
            specialHandlingRequirements:
                form.specialHandlingRequirements.trim() || null,
            priority: form.priority,
            pickupDateUtc: toUtc(form.pickupDate),
            expectedDeliveryUtc: toUtc(form.expectedDeliveryDate),
            deliveryTimeWindow: form.deliveryTimeWindow.trim() || null,
            additionalInstructions:
                form.additionalInstructions.trim() || null,
            status: form.status
        };

        if (
            !payload.origin ||
            !payload.destination ||
            !payload.cargoName ||
            !payload.packageCount ||
            !payload.totalWeightKg ||
            payload.packageCount < 1 ||
            payload.totalWeightKg <= 0
        ) {
            setFormError(
                "Complete all required fields with valid package and weight values."
            );
            return;
        }

        setSaving(true);

        try {
            const result = await bookingService.create(payload);

            setSuccess(
                `Booking created successfully. Tracking number: ${
                    result?.trackingNumber || "Created"
                }`
            );

            setForm(emptyForm);
            setShowForm(false);
            navigate("/app/bookings");
            setReloadToken((value) => value + 1);
        } catch (err) {
            setFormError(
                err.message || "Unable to create the booking."
            );
        } finally {
            setSaving(false);
        }
    }

    return (
        <section className="module-page booking-page">
            <div className="page-heading-row">
                <div>
                    <h1>Booking Management</h1>
                    <p>
                        Create and manage customer shipment bookings.
                    </p>
                </div>

                <div className="page-actions">
                    <button
                        type="button"
                        className="button-primary"
                        onClick={openCreateForm}
                    >
                        + Create Booking
                    </button>
                </div>
            </div>

            {success && (
                <div className="booking-success" role="status">
                    {success}
                </div>
            )}

            <div className="dashboard-card booking-list-card">
                <div className="booking-toolbar">
                    <input
                        type="search"
                        placeholder="Search booking ID or customer..."
                        aria-label="Search bookings"
                        value={search}
                        onChange={(event) => setSearch(event.target.value)}
                    />

                    <select
                        aria-label="Filter bookings by status"
                        value={status}
                        onChange={(event) => setStatus(event.target.value)}
                    >
                        <option value="All">All statuses</option>
                        <option value="Draft">Draft</option>
                        <option value="Confirmed">Confirmed</option>
                    </select>
                </div>

                {error && (
                    <div className="booking-error" role="alert">
                        Unable to load bookings: {error}
                    </div>
                )}

                {loading && (
                    <div className="dashboard-empty">
                        Loading bookings...
                    </div>
                )}

                {!loading && !error && bookings.length === 0 && (
                    <div className="dashboard-empty booking-empty">
                        <div className="empty-symbol" aria-hidden="true">▤</div>
                        <strong>No bookings found</strong>
                        <span>
                            Create a booking to see shipment requests here.
                        </span>
                        <button
                            type="button"
                            className="button-primary"
                            onClick={openCreateForm}
                        >
                            Create your first booking
                        </button>
                    </div>
                )}

                {!loading && !error && bookings.length > 0 && (
                    <div className="activity-table-wrap">
                        <table className="activity-table booking-table">
                            <thead>
                                <tr>
                                    <th>Booking ID</th>
                                    <th>Customer</th>
                                    <th>Route</th>
                                    <th>Cargo</th>
                                    <th>Weight</th>
                                    <th>Pickup date</th>
                                    <th>Priority</th>
                                    <th>Status</th>
                                </tr>
                            </thead>
                            <tbody>
                                {bookings.map((booking) => (
                                    <tr key={booking.id}>
                                        <td className="booking-reference">
                                            {booking.bookingId || "—"}
                                        </td>
                                        <td>{booking.customer || "—"}</td>
                                        <td>
                                            <div className="booking-route">
                                                <span>{booking.pickupLocation || "—"}</span>
                                                <span aria-hidden="true">↓</span>
                                                <span>{booking.deliveryLocation || "—"}</span>
                                            </div>
                                        </td>
                                        <td>
                                            {booking.cargoName ||
                                                booking.cargoType ||
                                                "—"}
                                        </td>
                                        <td>
                                            {booking.weight != null
                                                ? `${booking.weight} kg`
                                                : "—"}
                                        </td>
                                        <td>{formatDate(booking.pickupDate)}</td>
                                        <td>{booking.priority || "—"}</td>
                                        <td>
                                            <span className={statusClass(booking.status)}>
                                                {booking.status || "Unknown"}
                                            </span>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}

                {!loading && !error && bookings.length > 0 && (
                    <div className="booking-list-footer">
                        Showing {bookings.length} booking(s)
                    </div>
                )}
            </div>

            {showForm && (
                <div className="booking-form-overlay">
                    <section
                        className="booking-form-panel"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="booking-form-title"
                    >
                        <div className="booking-form-header">
                            <div>
                                <h2 id="booking-form-title">Create Booking</h2>
                                <p>
                                    Enter shipment, cargo and pickup details.
                                </p>
                            </div>

                            <button
                                type="button"
                                className="booking-close-button"
                                onClick={closeCreateForm}
                                disabled={saving}
                                aria-label="Close booking form"
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={handleSubmit}>
                            <div className="booking-form-body">
                                <div className="booking-form-section">
                                    <h3>Customer and route</h3>

                                    <div className="booking-form-grid">
                                        <label className="booking-field booking-field-full">
                                            Customer <span>*</span>
                                            <select
                                                name="customerId"
                                                value={form.customerId}
                                                onChange={handleInputChange}
                                                required
                                                disabled={customersLoading}
                                            >
                                                <option value="">
                                                    {customersLoading
                                                        ? "Loading customers..."
                                                        : "Select an active customer"}
                                                </option>
                                                {customers.map((customer) => (
                                                    <option
                                                        key={customer.id}
                                                        value={customer.id}
                                                    >
                                                        {customer.companyName}
                                                    </option>
                                                ))}
                                            </select>
                                        </label>

                                        <label className="booking-field">
                                            Pickup location <span>*</span>
                                            <input
                                                name="origin"
                                                value={form.origin}
                                                onChange={handleInputChange}
                                                maxLength={250}
                                                placeholder="Enter pickup location"
                                                required
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Delivery location <span>*</span>
                                            <input
                                                name="destination"
                                                value={form.destination}
                                                onChange={handleInputChange}
                                                maxLength={250}
                                                placeholder="Enter delivery location"
                                                required
                                            />
                                        </label>
                                    </div>
                                </div>

                                <div className="booking-form-section">
                                    <h3>Cargo information</h3>

                                    <div className="booking-form-grid">
                                        <label className="booking-field">
                                            Cargo name <span>*</span>
                                            <input
                                                name="cargoName"
                                                value={form.cargoName}
                                                onChange={handleInputChange}
                                                maxLength={200}
                                                placeholder="e.g. Electronic equipment"
                                                required
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Cargo category
                                            <input
                                                name="cargoCategory"
                                                value={form.cargoCategory}
                                                onChange={handleInputChange}
                                                maxLength={100}
                                                placeholder="e.g. Electronics"
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Package count <span>*</span>
                                            <input
                                                type="number"
                                                name="packageCount"
                                                value={form.packageCount}
                                                onChange={handleInputChange}
                                                min="1"
                                                step="1"
                                                required
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Total weight (kg) <span>*</span>
                                            <input
                                                type="number"
                                                name="totalWeightKg"
                                                value={form.totalWeightKg}
                                                onChange={handleInputChange}
                                                min="0.01"
                                                step="0.01"
                                                placeholder="Enter total weight"
                                                required
                                            />
                                        </label>
                                    </div>

                                    <div className="booking-subsection-label">
                                        Dimensions (cm) – optional
                                    </div>

                                    <div className="booking-form-grid booking-dimensions-grid">
                                        <label className="booking-field">
                                            Length
                                            <input
                                                type="number"
                                                name="lengthCm"
                                                value={form.lengthCm}
                                                onChange={handleInputChange}
                                                min="0.01"
                                                step="0.01"
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Width
                                            <input
                                                type="number"
                                                name="widthCm"
                                                value={form.widthCm}
                                                onChange={handleInputChange}
                                                min="0.01"
                                                step="0.01"
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Height
                                            <input
                                                type="number"
                                                name="heightCm"
                                                value={form.heightCm}
                                                onChange={handleInputChange}
                                                min="0.01"
                                                step="0.01"
                                            />
                                        </label>
                                    </div>

                                    <label className="booking-checkbox">
                                        <input
                                            type="checkbox"
                                            name="isFragile"
                                            checked={form.isFragile}
                                            onChange={handleInputChange}
                                        />
                                        Fragile cargo – special care required
                                    </label>

                                    <label className="booking-field booking-field-full">
                                        Special handling requirements
                                        <textarea
                                            name="specialHandlingRequirements"
                                            value={form.specialHandlingRequirements}
                                            onChange={handleInputChange}
                                            maxLength={500}
                                            rows={2}
                                            placeholder="Describe any special handling needs"
                                        />
                                    </label>
                                </div>

                                <div className="booking-form-section">
                                    <h3>Pickup and delivery</h3>

                                    <div className="booking-form-grid">
                                        <label className="booking-field">
                                            Pickup date <span>*</span>
                                            <input
                                                type="date"
                                                name="pickupDate"
                                                value={form.pickupDate}
                                                onChange={handleInputChange}
                                                required
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Expected delivery date
                                            <input
                                                type="date"
                                                name="expectedDeliveryDate"
                                                value={form.expectedDeliveryDate}
                                                onChange={handleInputChange}
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Delivery time window
                                            <input
                                                name="deliveryTimeWindow"
                                                value={form.deliveryTimeWindow}
                                                onChange={handleInputChange}
                                                maxLength={100}
                                                placeholder="e.g. 9 AM – 12 PM"
                                            />
                                        </label>

                                        <label className="booking-field">
                                            Priority <span>*</span>
                                            <select
                                                name="priority"
                                                value={form.priority}
                                                onChange={handleInputChange}
                                                required
                                            >
                                                <option value="Low">Low</option>
                                                <option value="Normal">Normal</option>
                                                <option value="High">High</option>
                                                <option value="Urgent">Urgent</option>
                                            </select>
                                        </label>

                                        <label className="booking-field">
                                            Booking status <span>*</span>
                                            <select
                                                name="status"
                                                value={form.status}
                                                onChange={handleInputChange}
                                                required
                                            >
                                                <option value="Draft">Draft</option>
                                                <option value="Confirmed">Confirmed</option>
                                            </select>
                                        </label>
                                    </div>

                                    <label className="booking-field booking-field-full">
                                        Additional instructions
                                        <textarea
                                            name="additionalInstructions"
                                            value={form.additionalInstructions}
                                            onChange={handleInputChange}
                                            maxLength={1000}
                                            rows={3}
                                            placeholder="Enter additional shipment instructions"
                                        />
                                    </label>
                                </div>

                                {customerError && (
                                    <div className="booking-error" role="alert">
                                        {customerError}
                                    </div>
                                )}

                                {customers.length === 0 &&
                                    !customersLoading &&
                                    !customerError && (
                                        <div className="booking-error" role="alert">
                                            No active customers are available.
                                            Add or activate a customer before creating a booking.
                                        </div>
                                    )}

                                {formError && (
                                    <div className="booking-error" role="alert">
                                        {formError}
                                    </div>
                                )}
                            </div>

                            <div className="booking-form-actions">
                                <button
                                    type="button"
                                    className="customer-button-secondary"
                                    onClick={closeCreateForm}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="button-primary"
                                    disabled={
                                        saving ||
                                        customersLoading ||
                                        customers.length === 0 ||
                                        Boolean(customerError)
                                    }
                                >
                                    {saving ? "Creating..." : "Create Booking"}
                                </button>
                            </div>
                        </form>
                    </section>
                </div>
            )}
        </section>
    );
}

export default BookingsPage;
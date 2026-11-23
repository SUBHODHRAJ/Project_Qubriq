import "../styles/customers.css";
import "../styles/dashboard.css";
import { useEffect, useState } from "react";
import { customerService } from "../services/customerService";

const emptyForm = {
    companyName: "",
    contactName: "",
    email: "",
    phone: "",
    address: ""
};

function CustomersPage() {
    const [customers, setCustomers] = useState([]);
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState("");
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState("");
    const [showForm, setShowForm] = useState(false);
    const [editingCustomer, setEditingCustomer] = useState(null);
    const [statusUpdatingId, setStatusUpdatingId] = useState(null);
    const [form, setForm] = useState(emptyForm);
    const [saving, setSaving] = useState(false);
    const [formError, setFormError] = useState("");
    const [success, setSuccess] = useState("");
    const [reloadToken, setReloadToken] = useState(0);

    useEffect(() => {
        let cancelled = false;

        async function loadCustomers() {
            setLoading(true);
            setError("");

            try {
                const result = await customerService.getAll(search, status);

                if (!cancelled) {
                    setCustomers(Array.isArray(result) ? result : []);
                }
            } catch (err) {
                if (!cancelled) {
                    setError(err.message);
                    setCustomers([]);
                }
            } finally {
                if (!cancelled) {
                    setLoading(false);
                }
            }
        }

        loadCustomers();

        return () => {
            cancelled = true;
        };
    }, [search, status, reloadToken]);

function handleEdit(customer) {
        setEditingCustomer(customer);

        setForm({
            companyName: customer.companyName || "",
            contactName: customer.contactName || "",
            email: customer.email || "",
            phone: customer.phone || "",
            address: customer.address || ""
        });

        setFormError("");
        setSuccess("");
        setShowForm(true);
    }

    async function handleStatusChange(customer) {
        const nextStatus = !customer.isActive;
        const action = nextStatus ? "activate" : "deactivate";

        if (!window.confirm(
            `Are you sure you want to ${action} ${customer.companyName}?`
        )) {
            return;
        }

        setStatusUpdatingId(customer.id);
        setSuccess("");
        setError("");

        try {
            await customerService.updateStatus(customer.id, nextStatus);

            setSuccess(
                `Customer ${nextStatus ? "activated" : "deactivated"} successfully.`
            );

            setReloadToken((value) => value + 1);
        } catch (err) {
            setError(`Unable to update customer status: ${err.message}`);
        } finally {
            setStatusUpdatingId(null);
        }
    }


    function handleInputChange(event) {
        const { name, value } = event.target;

        setForm((current) => ({
            ...current,
            [name]: value
        }));
    }

    function closeForm() {
        if (saving) return;

        setShowForm(false);
        setForm(emptyForm);
        setEditingCustomer(null);
        setFormError("");
    }

    async function handleSubmit(event) {
        event.preventDefault();
        setFormError("");
        setSuccess("");

        const payload = {
            companyName: form.companyName.trim(),
            contactName: form.contactName.trim() || null,
            email: form.email.trim() || null,
            phone: form.phone.trim() || null,
            address: form.address.trim() || null
        };

        if (payload.companyName.length < 2) {
            setFormError("Company name must contain at least 2 characters.");
            return;
        }

        setSaving(true);

        try {
            if (editingCustomer) {
                await customerService.update(editingCustomer.id, payload);
                setSuccess("Customer updated successfully.");
            } else {
                await customerService.create(payload);
                setSuccess("Customer created successfully.");
            }

            setShowForm(false);
            setForm(emptyForm);
            setEditingCustomer(null);
            setReloadToken((value) => value + 1);
        } catch (err) {
            setFormError(err.message || "Unable to create customer.");
        } finally {
            setSaving(false);
        }
    }

    return (
        <section className="module-page">
            <div className="page-heading-row">
                <div>
                    <h1>Customer Management</h1>
                    <p>Manage customer accounts and contact information.</p>
                </div>

                <div className="page-actions">
                    <button
                        type="button"
                        className="button-primary"
                        onClick={() => {
                            setSuccess("");
                            setFormError("");
                            setEditingCustomer(null);
                            setForm(emptyForm);
                            setShowForm(true);
                        }}
                    >
                        + Add Customer
                    </button>
                </div>
            </div>

            <div className="dashboard-card">
                <div className="customer-toolbar">
                    <input
                        type="search"
                        placeholder="Search customers..."
                        aria-label="Search customers"
                        value={search}
                        onChange={(event) => setSearch(event.target.value)}
                    />

                    <select
                        aria-label="Filter customers by status"
                        value={status}
                        onChange={(event) => setStatus(event.target.value)}
                    >
                        <option value="">All customers</option>
                        <option value="true">Active</option>
                        <option value="false">Inactive</option>
                    </select>
                </div>

                {success && (
                    <p role="status" className="customer-success">
                        {success}
                    </p>
                )}

                {error && (
                    <p role="alert" className="customer-error">
                        Unable to load customers: {error}
                    </p>
                )}

                {loading && (
                    <div className="dashboard-empty">Loading customers...</div>
                )}

                {!loading && !error && customers.length === 0 && (
                    <div className="dashboard-empty">
                        <div className="empty-symbol" aria-hidden="true">▤</div>
                        <strong>No customers found</strong>
                        <span>
                            Customer records will appear here once they are
                            added to the database.
                        </span>
                    </div>
                )}

                {!loading && !error && customers.length > 0 && (
                    <div className="activity-table-wrap">
                        <table className="activity-table">
                            <thead>
                                <tr>
                                    <th>Company</th>
                                    <th>Contact</th>
                                    <th>Email</th>
                                    <th>Phone</th>
                                    <th>Status</th>
                                    <th>Actions</th>
                                </tr>
                            </thead>
                            <tbody>
                                {customers.map((customer) => (
                                    <tr key={customer.id}>
                                        <td>{customer.companyName}</td>
                                        <td>{customer.contactName || "—"}</td>
                                        <td>{customer.email || "—"}</td>
                                        <td>{customer.phone || "—"}</td>
                                        <td>
                                            <span className={customer.isActive ? "customer-status-active" : "customer-status-inactive"}>
                                                {customer.isActive ? "Active" : "Inactive"}
                                            </span>
                                        </td>
                                        <td>
                                            <div className="customer-row-actions">
                                                <button
                                                    type="button"
                                                    className="customer-action-edit"
                                                    onClick={() => handleEdit(customer)}
                                                >
                                                    Edit
                                                </button>
                                                <button
                                                    type="button"
                                                    className={customer.isActive ? "customer-action-deactivate" : "customer-action-activate"}
                                                    disabled={statusUpdatingId === customer.id}
                                                    onClick={() => handleStatusChange(customer)}
                                                >
                                                    {statusUpdatingId === customer.id
                                                        ? "Updating..."
                                                        : customer.isActive ? "Deactivate" : "Activate"}
                                                </button>
                                            </div>
                                        </td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </div>
                )}
            </div>

            {showForm && (
                <div
                    className="customer-modal-overlay"
                    onMouseDown={(event) => {
                        if (event.target === event.currentTarget) closeForm();
                    }}
                >
                    <section
                        className="customer-modal"
                        role="dialog"
                        aria-modal="true"
                        aria-labelledby="customer-modal-title"
                    >
                        <div className="customer-modal-header">
                            <div>
                                <h2 id="customer-modal-title">{editingCustomer ? "Edit Customer" : "Add Customer"}</h2>
                                <p>Enter the customer's company and contact details.</p>
                            </div>

                            <button
                                type="button"
                                className="customer-close-button"
                                onClick={closeForm}
                                disabled={saving}
                                aria-label="Close form"
                            >
                                ×
                            </button>
                        </div>

                        <form onSubmit={handleSubmit}>
                            <div className="customer-form-grid">
                                <label className="customer-field customer-field-full">
                                    Company Name <span>*</span>
                                    <input
                                        name="companyName"
                                        value={form.companyName}
                                        onChange={handleInputChange}
                                        placeholder="Enter company name"
                                        minLength={2}
                                        maxLength={200}
                                        required
                                        autoFocus
                                    />
                                </label>

                                <label className="customer-field">
                                    Contact Person
                                    <input
                                        name="contactName"
                                        value={form.contactName}
                                        onChange={handleInputChange}
                                        placeholder="Enter contact name"
                                        maxLength={150}
                                    />
                                </label>

                                <label className="customer-field">
                                    Email Address
                                    <input
                                        type="email"
                                        name="email"
                                        value={form.email}
                                        onChange={handleInputChange}
                                        placeholder="name@company.com"
                                        maxLength={254}
                                    />
                                </label>

                                <label className="customer-field">
                                    Phone Number
                                    <input
                                        type="tel"
                                        name="phone"
                                        value={form.phone}
                                        onChange={handleInputChange}
                                        placeholder="Enter phone number"
                                        maxLength={30}
                                    />
                                </label>

                                <label className="customer-field customer-field-full">
                                    Address
                                    <textarea
                                        name="address"
                                        value={form.address}
                                        onChange={handleInputChange}
                                        placeholder="Enter company address"
                                        maxLength={500}
                                        rows={3}
                                    />
                                </label>
                            </div>

                            {formError && (
                                <p role="alert" className="customer-error customer-form-error">
                                    {formError}
                                </p>
                            )}

                            <div className="customer-form-actions">
                                <button
                                    type="button"
                                    className="customer-button-secondary"
                                    onClick={closeForm}
                                    disabled={saving}
                                >
                                    Cancel
                                </button>

                                <button
                                    type="submit"
                                    className="button-primary"
                                    disabled={saving}
                                >
                                    {saving ? "Saving..." : editingCustomer ? "Update Customer" : "Save Customer"}
                                </button>
                            </div>
                        </form>
                    </section>
                </div>
            )}
        </section>
    );
}

export default CustomersPage;

import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/auth.css";

function CustomerPortalPage() {
    const { user, logout } = useAuth();
    const navigate = useNavigate();

    function handleLogout() {
        logout();
        navigate("/login", { replace: true });
    }

    return (
        <main className="portal-page">
            <header className="portal-header">
                <strong>Qubriq</strong>
                <div>
                    <span>{user?.fullName || "Customer"}</span>
                    <button type="button" onClick={handleLogout}>
                        Sign out
                    </button>
                </div>
            </header>

            <section className="portal-content">
                <p className="auth-eyebrow">CUSTOMER PORTAL</p>
                <h1>Welcome, {user?.fullName || "Customer"}</h1>
                <p>Manage your bookings and follow your shipments.</p>

                <div className="portal-card">
                    <h2>My Bookings</h2>
                    <p>View your bookings and shipment history.</p>

                    <div className="portal-actions">
                        <Link
                            className="button-primary"
                            to="/customer/bookings"
                        >
                            View My Bookings
                        </Link>

                        <Link
                            className="button-primary"
                            to="/customer/bookings/new"
                        >
                            Create Booking
                        </Link>
                    </div>
                </div>
            </section>
        </main>
    );
}

export default CustomerPortalPage;

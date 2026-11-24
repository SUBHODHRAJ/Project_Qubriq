import { Link } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/auth.css";

function UnauthorizedPage() {
    const { logout } = useAuth();

    return (
        <main className="auth-page">
            <section className="auth-card">
                <h1>Access denied</h1>
                <p className="auth-description">
                    Your account does not have permission to access this page.
                </p>
                <Link to="/" className="auth-primary-link">Return home</Link>
                <button className="auth-secondary-button" onClick={logout}>
                    Sign out
                </button>
            </section>
        </main>
    );
}

export default UnauthorizedPage;

import { useState } from "react";
import { Link, Navigate, useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import { getRoleHome } from "../routes/roleHome";
import "../styles/auth.css";

function LoginPage() {
    const { login, isAuthenticated, roles } = useAuth();
    const navigate = useNavigate();
    const location = useLocation();

    const [email, setEmail] = useState("");
    const [password, setPassword] = useState("");
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState("");

    if (isAuthenticated) {
        return <Navigate to={getRoleHome(roles)} replace />;
    }

    async function handleSubmit(event) {
        event.preventDefault();
        setError("");
        setLoading(true);

        try {
            const result = await login(email.trim(), password);
            const destination = location.state?.from?.pathname;

            const home = getRoleHome(result.user.roles || []);

            if (home === "/unauthorized") {
                setError("Your account does not have an assigned Qubriq role.");
                return;
            }

            navigate(destination || home, { replace: true });
        } catch (err) {
            setError(err.message || "Login failed. Check your credentials.");
        } finally {
            setLoading(false);
        }
    }

    return (
        <main className="auth-page">
            <section className="auth-card">
                <Link to="/" className="auth-brand">Qubriq</Link>
                <p className="auth-eyebrow">LOGISTICS OPERATING SYSTEM</p>

                <h1>Welcome back</h1>
                <p className="auth-description">
                    Sign in to access your Qubriq workspace.
                </p>

                <form onSubmit={handleSubmit} className="auth-form">
                    <label>
                        Email address
                        <input
                            type="email"
                            value={email}
                            onChange={event => setEmail(event.target.value)}
                            placeholder="Enter your registered email"
                            autoComplete="username"
                            maxLength={254}
                            required
                        />
                    </label>

                    <label>
                        Password
                        <input
                            type="password"
                            value={password}
                            onChange={event => setPassword(event.target.value)}
                            placeholder="Enter your password"
                            autoComplete="current-password"
                            maxLength={200}
                            required
                        />
                    </label>

                    {error && (
                        <div className="auth-error" role="alert">
                            {error}
                        </div>
                    )}

                    <button type="submit" disabled={loading}>
                        {loading ? "Signing in..." : "Sign in"}
                    </button>
                </form>

                <p className="auth-footer">
                    Need an account? <Link to="/signup">Contact your administrator</Link>
                </p>

                <Link to="/" className="auth-back-link">Back to home</Link>
            </section>
        </main>
    );
}

export default LoginPage;

import { Link } from "react-router-dom";
import "../styles/auth.css";

function SignupPage() {
    return (
        <main className="auth-page">
            <section className="auth-card">
                <Link to="/" className="auth-brand">Qubriq</Link>
                <p className="auth-eyebrow">ACCOUNT ACCESS</p>

                <h1>Get access to Qubriq</h1>

                <p className="auth-description">
                    Qubriq accounts are created by an authorized platform
                    administrator to ensure the correct role and access
                    permissions are assigned.
                </p>

                <div className="auth-information">
                    <strong>How to get an account</strong>
                    <ol>
                        <li>Contact your Qubriq administrator.</li>
                        <li>Provide your name and registered email address.</li>
                        <li>Receive your account credentials securely.</li>
                        <li>Sign in to access your assigned workspace.</li>
                    </ol>
                </div>

                <Link to="/login" className="auth-primary-link">
                    Go to sign in
                </Link>

                <Link to="/" className="auth-back-link">Back to home</Link>
            </section>
        </main>
    );
}

export default SignupPage;

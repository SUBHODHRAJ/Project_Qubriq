import { Link } from "react-router-dom";

function NotFoundPage() {
    return (
        <main className="placeholder-page">
            <h1>404 - Page Not Found</h1>
            <p>The page you requested does not exist.</p>
            <Link to="/">Return to Home</Link>
        </main>
    );
}

export default NotFoundPage;

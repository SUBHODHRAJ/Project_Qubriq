import { Link } from "react-router-dom";
import "../styles/landing.css";

const features = [
    {
        icon: "01",
        title: "Smart Shipment Management",
        description:
            "Manage bookings, shipments and delivery operations from one centralized platform."
    },
    {
        icon: "02",
        title: "Fleet & Capacity Planning",
        description:
            "Improve vehicle utilization and organize fleet operations with intelligent planning."
    },
    {
        icon: "03",
        title: "Predictive Intelligence",
        description:
            "Identify potential delivery delays and operational risks before they escalate."
    },
    {
        icon: "04",
        title: "Complete Visibility",
        description:
            "Monitor logistics activities and maintain transparency across your operations."
    }
];

function LandingPage() {
    return (
        <div className="landing-page">
            <header className="landing-header">
                <Link to="/" className="landing-brand">
                    <span className="brand-mark">Q</span>
                    <span>QUBRIQ</span>
                </Link>

                <nav className="landing-nav">
                    <a href="#features">Features</a>
                    <a href="#about">About</a>
                    <a href="#contact">Contact</a>
                </nav>

                <div className="header-actions">
                    <Link to="/login" className="login-link">
                        Login
                    </Link>
                    <Link to="/signup" className="primary-button">
                        Get Started
                    </Link>
                </div>
            </header>

            <main>
                <section className="hero-section">
                    <div className="hero-content">
                        <span className="hero-label">
                            LOGISTICS OPERATING SYSTEM
                        </span>

                        <h1>
                            Intelligent Logistics.
                            <span> Complete Transparency.</span>
                        </h1>

                        <p>
                            Simplify logistics operations with one connected
                            platform for shipment management, fleet planning,
                            compliance and predictive intelligence.
                        </p>

                        <div className="hero-actions">
                            <Link to="/signup" className="primary-button hero-button">
                                Explore Qubriq <span>→</span>
                            </Link>

                            <Link to="/login" className="secondary-button">
                                Sign in to your account
                            </Link>
                        </div>

                        <div className="hero-trust">
                            <span className="trust-dot"></span>
                            One platform. Connected operations.
                        </div>
                    </div>

                    <div className="hero-visual">
                        <div className="visual-glow"></div>

                        <div className="visual-card">
                            <div className="visual-card-header">
                                <div>
                                    <span className="visual-eyebrow">
                                        QUBRIQ OVERVIEW
                                    </span>
                                    <h3>Operations Center</h3>
                                </div>
                                <span className="live-indicator">
                                    <span></span> LIVE
                                </span>
                            </div>

                            <div className="visual-stats">
                                <div className="visual-stat">
                                    <span>Shipments</span>
                                    <strong>1,284</strong>
                                    <small>Active operations</small>
                                </div>
                                <div className="visual-stat">
                                    <span>Fleet</span>
                                    <strong>86%</strong>
                                    <small>Utilization</small>
                                </div>
                            </div>

                            <div className="visual-route">
                                <div className="route-heading">
                                    <span>Shipment overview</span>
                                    <span>Route status</span>
                                </div>

                                <div className="route-line">
                                    <div className="route-point">
                                        <span className="route-circle origin"></span>
                                        <div>
                                            <strong>Origin</strong>
                                            <small>Warehouse</small>
                                        </div>
                                    </div>

                                    <div className="route-track">
                                        <span></span>
                                    </div>

                                    <div className="route-point">
                                        <span className="route-circle destination"></span>
                                        <div>
                                            <strong>Destination</strong>
                                            <small>Delivery hub</small>
                                        </div>
                                    </div>
                                </div>
                            </div>

                            <div className="visual-bottom">
                                <span>Operational visibility</span>
                                <strong>Connected with Qubriq</strong>
                            </div>
                        </div>

                        <div className="floating-tag tag-top">
                            <span className="tag-icon">✓</span>
                            <div>
                                <strong>Smart Planning</strong>
                                <small>Optimized operations</small>
                            </div>
                        </div>

                        <div className="floating-tag tag-bottom">
                            <span className="tag-icon tag-blue">↗</span>
                            <div>
                                <strong>Real-time Visibility</strong>
                                <small>Connected logistics</small>
                            </div>
                        </div>
                    </div>
                </section>

                <section className="features-section" id="features">
                    <div className="section-heading">
                        <span className="section-label">WHAT QUBRIQ OFFERS</span>
                        <h2>Everything your logistics operations need.</h2>
                        <p>
                            Bring your logistics workflow together with
                            connected tools designed for smarter operations.
                        </p>
                    </div>

                    <div className="features-grid">
                        {features.map((feature) => (
                            <article className="feature-card" key={feature.title}>
                                <span className="feature-number">
                                    {feature.icon}
                                </span>
                                <h3>{feature.title}</h3>
                                <p>{feature.description}</p>
                                <span className="feature-arrow">↗</span>
                            </article>
                        ))}
                    </div>
                </section>

                <section className="about-section" id="about">
                    <div>
                        <span className="section-label">ONE CONNECTED PLATFORM</span>
                        <h2>From planning to delivery, all in one place.</h2>
                    </div>
                    <p>
                        Qubriq brings logistics workflows together to help
                        teams coordinate shipments, manage resources and
                        make informed operational decisions.
                    </p>
                </section>
            </main>

            <footer className="landing-footer" id="contact">
                <Link to="/" className="landing-brand footer-brand">
                    <span className="brand-mark">Q</span>
                    <span>QUBRIQ</span>
                </Link>

                <p>Intelligent Logistics. Complete Transparency.</p>
                <span>© {new Date().getFullYear()} Qubriq. All rights reserved.</span>
            </footer>
        </div>
    );
}

export default LandingPage;

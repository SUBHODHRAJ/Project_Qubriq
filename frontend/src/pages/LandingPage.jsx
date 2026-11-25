import { Link } from "react-router-dom";
import React, { useState } from "react";
import "../styles/landing.css";

const features = [
    {
        icon: "01",
        tag: "DISPATCH & BOOKINGS",
        title: "Smart Shipment Management",
        description:
            "Manage bookings, shipments, E-Way Bill compliance, and delivery operations from one centralized command center."
    },
    {
        icon: "02",
        tag: "3D LOAD & ROUTING",
        title: "Fleet & Capacity Planning",
        description:
            "Maximize vehicle cubing utilization and organize multi-stop corridor routes with intelligent capacity planning."
    },
    {
        icon: "03",
        tag: "ML RISK SCORING",
        title: "Predictive Intelligence",
        description:
            "Identify potential transit delays, weather bottlenecks, and SLA risks before they escalate."
    },
    {
        icon: "04",
        tag: "END-TO-END TELEMETRY",
        title: "Complete Visibility",
        description:
            "Monitor live fleet milestones and maintain real-time operational transparency across hubs and recipients."
    }
];

const metrics = [
    { value: "99.4%", label: "On-time SLA tracking" },
    { value: "+24%", label: "Average fleet cubing gain" },
    { value: "< 2 min", label: "Automated compliance dispatch" },
    { value: "24 / 7", label: "Live corridor telemetry" }
];

function LandingPage() {
    const [activeRole, setActiveRole] = useState("dispatcher");

    const roleViews = {
        dispatcher: {
            tab: "01. Dispatcher",
            badge: "COMMAND CENTER",
            title: "Orchestrate multi-corridor dispatch with zero blind spots.",
            description:
                "Assign freight loads, validate GST E-Way Bills before departure, and catch ML-predicted corridor delays before they impact SLAs.",
            points: [
                "Live corridor risk scoring & automated delay alerts",
                "One-click GST E-Way Bill compliance verification",
                "Real-time vehicle assignment across regional hubs",
            ],
            cta: "Open Dispatcher Console",
            previewHeader: "CORRIDOR TR-08 • COIMBATORE → CHENNAI",
            previewStatus: "ON SCHEDULE",
            previewMetrics: [
                { label: "Active Consignments", value: "6 Cargo Units" },
                { label: "E-Way Bill Status", value: "Verified • Part-B Active" },
                { label: "Predicted ETA", value: "18:40 IST (98% Confidence)" },
            ],
        },
        fleet: {
            tab: "02. Fleet Operator",
            badge: "3D LOAD & FLEET",
            title: "Maximize truck cubing with 3D bin-packing optimization.",
            description:
                "Eliminate wasted trailer volume and axle-weight imbalances using automated 3D cargo placement and multi-stop route sequencing.",
            points: [
                "Interactive 3D container & truck load visualization",
                "Weight-distribution & stackability constraint checks",
                "Automated fuel & turnaround efficiency tracking",
            ],
            cta: "Explore Fleet Workspace",
            previewHeader: "VEHICLE TR-08 • 3D CAPACITY SYNC",
            previewStatus: "86.8% CUBED",
            previewMetrics: [
                { label: "Loaded Cargo", value: "C-101 to C-106 Packed" },
                { label: "Axle Balance", value: "Optimal • Within Limits" },
                { label: "Route Distance", value: "506 km • NH-544 Corridor" },
            ],
        },
        recipient: {
            tab: "03. Recipient Portal",
            badge: "CLIENT VISIBILITY",
            title: "Give recipients self-serve tracking and instant proof of delivery.",
            description:
                "Replace manual status calls with live milestone timelines, gate-in notifications, and digital POD verification.",
            points: [
                "Live map milestones with dynamic ETA updates",
                "Instant digital Proof of Delivery (e-POD) downloads",
                "Automated SMS & portal alerts on arrival",
            ],
            cta: "Launch Recipient View",
            previewHeader: "SHIPMENT #QB-8841 • RECIPIENT VIEW",
            previewStatus: "IN TRANSIT",
            previewMetrics: [
                { label: "Current Checkpoint", value: "Salem Transit Hub" },
                { label: "Handling", value: "Temperature & Seal Verified" },
                { label: "Delivery Window", value: "Today, 6:15 PM – 7:00 PM" },
            ],
        },
    };

    const currentRole = roleViews[activeRole];
    return (
        <div className="landing-page">
            <header className="landing-header">
                <Link to="/" className="landing-brand">
                    <span className="brand-mark">Q</span>
                    <span>QUBRIQ</span>
                </Link>

                <nav className="landing-nav">
                    <a href="#features">Features</a>
                    <a href="#platform">Platform</a>
                    <a href="#about">About</a>
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
                    <div className="hero-grid-bg" aria-hidden="true"></div>

                    <div className="hero-content">
                        <div className="hero-badge">
                            <span className="badge-pulse"></span>
                            LOGISTICS OPERATING SYSTEM
                        </div>

                        <h1>
                            Intelligent Logistics.
                            <span> Complete Transparency.</span>
                        </h1>

                        <p>
                            Unify dispatch, fleet capacity planning, compliance,
                            and predictive delay intelligence in one connected
                            logistics operating platform.
                        </p>

                        <div className="hero-actions">
                            <Link to="/signup" className="primary-button hero-button">
                                Explore Qubriq <span className="btn-arrow">→</span>
                            </Link>

                            <Link to="/login" className="secondary-button hero-button">
                                Sign in to workspace
                            </Link>
                        </div>

                        <div className="hero-trust">
                            <span className="trust-dot"></span>
                            <span>Built for dispatchers, fleet operators, and enterprise supply chains.</span>
                        </div>
                    </div>

                    <div className="hero-visual">
                        <div className="visual-glow"></div>
                            <img
                            src="/images/hero-graphic.png"
                            alt="Qubriq Operations Command Center"
                            className="hero-graphic-image"
                        />
                    </div>
                </section>

                <section className="metrics-strip" id="platform">
                    <div className="metrics-grid">
                        {metrics.map((item) => (
                            <div className="metric-item" key={item.label}>
                                <strong>{item.value}</strong>
                                <span>{item.label}</span>
                            </div>
                        ))}
                    </div>
                </section>

                <section id="platform" className="roles-section">
                <div className="section-header">
                    <span className="section-eyebrow">BUILT FOR EVERY OPERATOR</span>
                    <h2>One platform, tailored to every logistics role.</h2>
                </div>

                <div className="role-tabs" role="tablist">
                    {Object.entries(roleViews).map(([key, role]) => (
                        <button
                            key={key}
                            type="button"
                            role="tab"
                            aria-selected={activeRole === key}
                            className={`role-tab-btn ${activeRole === key ? "active" : ""}`}
                            onClick={() => setActiveRole(key)}
                        >
                            {role.tab}
                        </button>
                    ))}
                </div>

                <div className="role-panel">
                    <div className="role-info">
                        <span className="role-badge">{currentRole.badge}</span>
                        <h3>{currentRole.title}</h3>
                        <p>{currentRole.description}</p>
                        <ul className="role-points">
                            {currentRole.points.map((point) => (
                                <li key={point}>{point}</li>
                            ))}
                        </ul>
                        <Link to="/login" className="btn-primary role-cta">
                            {currentRole.cta} →
                        </Link>
                    </div>

                    <div className="role-preview-card">
                        <div className="role-preview-top">
                            <span className="role-preview-code">{currentRole.previewHeader}</span>
                            <span className="role-status-pill">{currentRole.previewStatus}</span>
                        </div>
                        <div className="role-preview-list">
                            {currentRole.previewMetrics.map((item) => (
                                <div key={item.label} className="role-metric-row">
                                    <span className="role-metric-label">{item.label}</span>
                                    <strong className="role-metric-value">{item.value}</strong>
                                </div>
                            ))}
                        </div>
                    </div>
                </div>
            </section>

                <section className="features-section" id="features">
                    <div className="section-heading">
                        <span className="section-label">WHAT QUBRIQ OFFERS</span>
                        <h2>Everything your logistics operations need.</h2>
                        <p>
                            Bring your entire freight workflow together with
                            connected tools engineered for speed, compliance,
                            and operational clarity.
                        </p>
                    </div>

                    <div className="features-grid">
                        {features.map((feature) => (
                            <article className="feature-card" key={feature.title}>
                                <div className="feature-card-top">
                                    <span className="feature-number">
                                        {feature.icon}
                                    </span>
                                    <span className="feature-tag">{feature.tag}</span>
                                </div>
                                <h3>{feature.title}</h3>
                                <p>{feature.description}</p>
                                <span className="feature-arrow">↗</span>
                            </article>
                        ))}
                    </div>
                </section>

                <section className="about-wrapper" id="about">
                    <div className="about-section">
                        <div className="about-copy">
                            <span className="section-label about-label">ONE CONNECTED PLATFORM</span>
                            <h2>From planning to delivery, all in one place.</h2>
                            <p>
                                Qubriq unifies bookings, fleet allocation, compliance
                                documentation, and recipient portals so operations teams
                                can make faster, data-backed decisions.
                            </p>
                        </div>
                        

                        <div className="about-cta">
                            <Link to="/signup" className="primary-button hero-button">
                                Launch Operations Console <span>→</span>
                            </Link>
                            <Link to="/login" className="about-secondary-link">
                                Existing team member? Sign in
                            </Link>
                        </div>
                    </div>
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
import { useEffect, useRef, useState } from "react";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/dashboard.css";

const navSections = [
    {
        title: "Core Operations",
        items: [
            { label: "Overview", path: "/app/overview", icon: "overview" },
            { label: "Bookings", path: "/app/bookings", icon: "bookings" },
            { label: "Customers", path: "/app/customers", icon: "customers", adminOnly: true },
            { label: "Fleet & Planning", path: "/app/fleet", icon: "fleet" },
            { label: "Load Optimization", path: "/app/load-optimization", icon: "load" },
            { label: "Route & Transportation", path: "/app/routes", icon: "routes" },
            { label: "Live Tracking", path: "/app/tracking", icon: "tracking" }
        ]
    },
    {
        title: "Intelligence & Comms",
        items: [
            { label: "Predictive Intelligence", path: "/app/predictions", icon: "predictive" },
            { label: "Network Intelligence", path: "/app/network", icon: "network" },
            { label: "WhatsApp Agent", path: "/app/whatsapp", icon: "whatsapp" }
        ]
    },
    {
        title: "Governance & System",
        items: [
            { label: "Compliance", path: "/app/compliance", icon: "compliance" },
            { label: "Finance", path: "/app/finance", icon: "finance", adminOnly: true },
            { label: "Analytics", path: "/app/analytics", icon: "analytics" },
            { label: "Settings", path: "/app/settings", icon: "settings", adminOnly: true }
        ]
    }
];

function NavIcon({ name }) {
    const props = {
        width: 16,
        height: 16,
        viewBox: "0 0 24 24",
        fill: "none",
        stroke: "currentColor",
        strokeWidth: 1.9,
        strokeLinecap: "round",
        strokeLinejoin: "round",
        "aria-hidden": "true"
    };

    switch (name) {
        case "overview":
            return <svg {...props}><rect x="3" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="3" width="7" height="7" rx="1.5" /><rect x="14" y="14" width="7" height="7" rx="1.5" /><rect x="3" y="14" width="7" height="7" rx="1.5" /></svg>;
        case "bookings":
            return <svg {...props}><path d="M9 5H7a2 2 0 0 0-2 2v12a2 2 0 0 0 2 2h10a2 2 0 0 0 2-2V7a2 2 0 0 0-2-2h-2" /><rect x="9" y="3" width="6" height="4" rx="1" /><path d="M9 12h6M9 16h4" /></svg>;
        case "customers":
            return <svg {...props}><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2" /><circle cx="9" cy="7" r="4" /><path d="M22 21v-2a4 4 0 0 0-3-3.87M16 3.13a4 4 0 0 1 0 7.75" /></svg>;
        case "fleet":
            return <svg {...props}><path d="M14 18V6a2 2 0 0 0-2-2H4a2 2 0 0 0-2 2v11a1 1 0 0 0 1 1h2" /><path d="M15 18H9" /><path d="M19 18h2a1 1 0 0 0 1-1v-3.65a1 1 0 0 0-.22-.624l-3.48-4.35A1 1 0 0 0 17.52 8H14" /><circle cx="17" cy="18" r="2" /><circle cx="7" cy="18" r="2" /></svg>;
        case "load":
            return <svg {...props}><path d="M21 16V8a2 2 0 0 0-1-1.73l-7-4a2 2 0 0 0-2 0l-7 4A2 2 0 0 0 3 8v8a2 2 0 0 0 1 1.73l7 4a2 2 0 0 0 2 0l7-4A2 2 0 0 0 21 16z" /><polyline points="3.27 6.96 12 12.01 20.73 6.96" /><line x1="12" y1="22.08" x2="12" y2="12" /></svg>;
        case "compliance":
            return <svg {...props}><path d="M12 22s8-4 8-10V5l-8-3-8 3v7c0 6 8 10 8 10z" /><path d="m9 12 2 2 4-4" /></svg>;
        case "routes":
            return <svg {...props}><circle cx="6" cy="19" r="3" /><path d="M9 19h8.5a3.5 3.5 0 0 0 0-7h-11a3.5 3.5 0 0 1 0-7H15" /><circle cx="18" cy="5" r="3" /></svg>;
        case "tracking":
            return <svg {...props}><circle cx="12" cy="12" r="2" /><path d="M16.24 7.76a6 6 0 0 1 0 8.49m-8.48-.01a6 6 0 0 1 0-8.49m11.31-2.82a10 10 0 0 1 0 14.14m-14.14 0a10 10 0 0 1 0-14.14" /></svg>;
        case "predictive":
            return <svg {...props}><polyline points="22 7 13.5 15.5 8.5 10.5 2 17" /><polyline points="16 7 22 7 22 13" /></svg>;
        case "network":
            return <svg {...props}><circle cx="12" cy="5" r="2.5" /><circle cx="5" cy="19" r="2.5" /><circle cx="19" cy="19" r="2.5" /><path d="M10.5 7.2 6.5 16.8M13.5 7.2l4 9.6M7.5 19h9" /></svg>;
        case "whatsapp":
            return <svg {...props}><path d="M21 11.5a8.38 8.38 0 0 1-.9 3.8 8.5 8.5 0 0 1-7.6 4.7 8.38 8.38 0 0 1-3.8-.9L3 21l1.9-5.7a8.38 8.38 0 0 1-.9-3.8 8.5 8.5 0 0 1 4.7-7.6 8.38 8.38 0 0 1 3.8-.9h.5a8.48 8.48 0 0 1 8 8v.5z" /></svg>;
        case "finance":
            return <svg {...props}><path d="M6 3h12M6 8h12M6 13l8.5 8M6 13h3a4 4 0 0 0 0-8" /></svg>;
        case "analytics":
            return <svg {...props}><line x1="18" y1="20" x2="18" y2="10" /><line x1="12" y1="20" x2="12" y2="4" /><line x1="6" y1="20" x2="6" y2="14" /></svg>;
        default:
            return <svg {...props}><circle cx="12" cy="12" r="3" /><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06a1.65 1.65 0 0 0 .33-1.82 1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06a1.65 1.65 0 0 0 1.82.33H9a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06a1.65 1.65 0 0 0-.33 1.82V9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z" /></svg>;
    }
}

function DashboardLayout() {
    const { user, roles, hasRole, logout } = useAuth();
    const navigate = useNavigate();
    const searchInputRef = useRef(null);
    const [searchQuery, setSearchQuery] = useState("");
    const [searchOpen, setSearchOpen] = useState(false);

    const isAdmin = hasRole("Admin");

    const visibleSections = navSections
        .map((section) => ({
            ...section,
            items: section.items.filter((item) => !item.adminOnly || isAdmin)
        }))
        .filter((section) => section.items.length > 0);

    const allVisibleItems = visibleSections.flatMap((s) => s.items);
    const filteredResults = searchQuery.trim()
        ? allVisibleItems.filter((item) =>
              item.label.toLowerCase().includes(searchQuery.trim().toLowerCase())
          )
        : [];

    const roleLabel = roles.join(", ") || "Dispatcher";
    const displayName = user?.fullName || "User";
    const initials = displayName
        .split(" ")
        .filter(Boolean)
        .slice(0, 2)
        .map((part) => part[0].toUpperCase())
        .join("") || "U";

    useEffect(() => {
        function onKeyDown(e) {
            if ((e.ctrlKey || e.metaKey) && e.key.toLowerCase() === "k") {
                e.preventDefault();
                searchInputRef.current?.focus();
                setSearchOpen(true);
            } else if (e.key === "Escape") {
                setSearchOpen(false);
            }
        }
        window.addEventListener("keydown", onKeyDown);
        return () => window.removeEventListener("keydown", onKeyDown);
    }, []);

    function handleLogout() {
        logout();
        navigate("/login", { replace: true });
    }

    function handleJumpToModule(path) {
        setSearchQuery("");
        setSearchOpen(false);
        navigate(path);
    }

    return (
        <div className="dashboard-shell">
            <aside className="dashboard-sidebar">
                <div className="sidebar-brand">
                    <div className="sidebar-brand-row">
                        <span className="sidebar-logo-mark" aria-hidden="true">Q</span>
                        <div>
                            <div className="sidebar-brand-name">Qubriq</div>
                            <div className="sidebar-brand-caption">Logistics Operating System</div>
                        </div>
                    </div>
                </div>

                <nav className="sidebar-navigation" aria-label="Main navigation">
                    {visibleSections.map((section) => (
                        <div className="sidebar-group" key={section.title}>
                            <div className="sidebar-group-title">{section.title}</div>
                            {section.items.map((item) => (
                                <NavLink
                                    key={item.path}
                                    to={item.path}
                                    className={({ isActive }) =>
                                        `sidebar-link${isActive ? " active" : ""}`
                                    }
                                >
                                    <span className="sidebar-icon">
                                        <NavIcon name={item.icon} />
                                    </span>
                                    <span className="sidebar-link-text">{item.label}</span>
                                </NavLink>
                            ))}
                        </div>
                    ))}
                </nav>

                <div className="sidebar-footer">
                    <span className="sidebar-status-dot" />
                    <span>Qubriq Control v1.0</span>
                </div>
            </aside>

            <div className="dashboard-main">
                <header className="dashboard-topbar">
                    <div className="global-search-wrap">
                        <div className="global-search">
                            <svg
                                className="global-search-icon"
                                width="15"
                                height="15"
                                viewBox="0 0 24 24"
                                fill="none"
                                stroke="currentColor"
                                strokeWidth="2"
                                strokeLinecap="round"
                                strokeLinejoin="round"
                                aria-hidden="true"
                            >
                                <circle cx="11" cy="11" r="8" />
                                <line x1="21" y1="21" x2="16.65" y2="16.65" />
                            </svg>
                            <input
                                ref={searchInputRef}
                                type="search"
                                value={searchQuery}
                                onFocus={() => setSearchOpen(true)}
                                onChange={(e) => {
                                    setSearchQuery(e.target.value);
                                    setSearchOpen(true);
                                }}
                                placeholder="Jump to module, booking, or shipment..."
                                aria-label="Global search"
                            />
                            <kbd className="search-kbd">Ctrl K</kbd>
                        </div>

                        {searchOpen && searchQuery.trim() && (
                            <div className="global-search-dropdown" role="listbox">
                                {filteredResults.length > 0 ? (
                                    filteredResults.map((item) => (
                                        <button
                                            key={item.path}
                                            type="button"
                                            className="search-result-item"
                                            onClick={() => handleJumpToModule(item.path)}
                                        >
                                            <NavIcon name={item.icon} />
                                            <span>{item.label}</span>
                                            <small>{item.path}</small>
                                        </button>
                                    ))
                                ) : (
                                    <div className="search-result-empty">
                                        No matching module for "{searchQuery}"
                                    </div>
                                )}
                            </div>
                        )}
                    </div>

                    <div className="topbar-actions">
                        <div className="workspace-pill" title="Active Operations Hub">
                            <span className="workspace-dot" />
                            <span>Chennai Hub • Ops</span>
                        </div>

                        <div className="topbar-divider" />

                        <div className="profile-placeholder">
                            <span className="profile-avatar">{initials}</span>

                            <div className="profile-label">
                                <strong>{displayName}</strong>
                                <span className="profile-role-badge">{roleLabel}</span>
                            </div>

                            <button
                                type="button"
                                className="topbar-signout-btn"
                                onClick={handleLogout}
                                aria-label="Sign out"
                                title="Sign out"
                            >
                                <svg
                                    width="14"
                                    height="14"
                                    viewBox="0 0 24 24"
                                    fill="none"
                                    stroke="currentColor"
                                    strokeWidth="2"
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    aria-hidden="true"
                                >
                                    <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
                                    <polyline points="16 17 21 12 16 7" />
                                    <line x1="21" y1="12" x2="9" y2="12" />
                                </svg>
                                <span>Sign out</span>
                            </button>
                        </div>
                    </div>
                </header>

                <main className="dashboard-content">
                    <Outlet />
                </main>
            </div>
        </div>
    );
}

export default DashboardLayout;
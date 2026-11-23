import { NavLink, Outlet } from "react-router-dom";
import "../styles/dashboard.css";

const navigation = [
    {
        label: "Overview",
        path: "/app/overview",
        icon: "▦"
    },
    {
        label: "Bookings",
        path: "/app/bookings",
        icon: "▤"
    },
    {
        label: "Customers",
        path: "/app/customers",
        icon: "♙"
    },
    {
        label: "Fleet & Planning",
        path: "/app/fleet",
        icon: "▣"
    },
    {
        label: "Load Optimization",
        path: "/app/load-optimization",
        icon: "▧"
    },
    {
        label: "Compliance",
        path: "/app/compliance",
        icon: "✓"
    },
    {
        label: "Route & Transportation",
        path: "/app/routes",
        icon: "⌖"
    },
    {
        label: "Live Tracking",
        path: "/app/tracking",
        icon: "◎"
    },
    {
        label: "Predictive Intelligence",
        path: "/app/predictions",
        icon: "⌁"
    },
    {
        label: "Network Intelligence",
        path: "/app/network",
        icon: "◇"
    },
    {
        label: "WhatsApp Agent",
        path: "/app/whatsapp",
        icon: "◉"
    },
    {
        label: "Finance",
        path: "/app/finance",
        icon: "₹"
    },
    {
        label: "Analytics",
        path: "/app/analytics",
        icon: "▥"
    },
    {
        label: "Settings",
        path: "/app/settings",
        icon: "⚙"
    }
];

function DashboardLayout() {
    return (
        <div className="dashboard-shell">
            <aside className="dashboard-sidebar">
                <div className="sidebar-brand">
                    <div className="sidebar-brand-name">Qubriq</div>
                    <div className="sidebar-brand-caption">
                        Logistics operating system
                    </div>
                </div>

                <nav className="sidebar-navigation" aria-label="Main navigation">
                    {navigation.map((item) => (
                        <NavLink
                            key={item.path}
                            to={item.path}
                            className={({ isActive }) =>
                                `sidebar-link${isActive ? " active" : ""}`
                            }
                        >
                            <span className="sidebar-icon" aria-hidden="true">
                                {item.icon}
                            </span>
                            <span>{item.label}</span>
                        </NavLink>
                    ))}
                </nav>

                <div className="sidebar-footer">
                    Qubriq Logistics Platform
                </div>
            </aside>

            <div className="dashboard-main">
                <header className="dashboard-topbar">
                    <div className="global-search">
                        <span aria-hidden="true">⌕</span>
                        <input
                            type="search"
                            placeholder="Global search"
                            aria-label="Global search"
                        />
                    </div>

                    <div className="topbar-actions">
                        <button
                            type="button"
                            className="organization-selector"
                            disabled
                            title="Organization selection will be connected later"
                        >
                            Select organization
                            <span aria-hidden="true">⌄</span>
                        </button>

                        <div className="profile-placeholder" aria-hidden="true">
                            <span className="profile-avatar">U</span>
                            <span className="profile-label">User profile</span>
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

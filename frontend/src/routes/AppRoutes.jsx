import { Routes, Route, Navigate } from "react-router-dom";
import LandingPage from "../pages/LandingPage";
import LoginPage from "../pages/LoginPage";
import SignupPage from "../pages/SignupPage";
import NotFoundPage from "../pages/NotFoundPage";
import DashboardLayout from "../layouts/DashboardLayout";
import CommandCenterPage from "../pages/CommandCenterPage";
import CustomersPage from "../pages/CustomersPage";
import ModulePage from "../pages/ModulePage";

const modules = [
    {
        path: "bookings",
        title: "Booking & Order Management",
        description: "Create and manage shipment bookings from request to dispatch."
    },
    {
        path: "fleet",
        title: "Fleet & Capacity Planning",
        description: "Manage vehicles, driver assignments and shipment allocation."
    },
    {
        path: "load-optimization",
        title: "3D Load Optimization",
        description: "Priority-aware 3D cargo packing for the selected vehicle and shipment group."
    },
    {
        path: "compliance",
        title: "Compliance & E-Way Bill Management",
        description: "Manage shipment documents, E-Way Bill validity and compliance checks."
    },
    {
        path: "routes",
        title: "Route & Transportation Planning",
        description: "Plan trips, compare routes and check delivery feasibility and E-Way Bill validity."
    },
    {
        path: "tracking",
        title: "Live Tracking",
        description: "Track active vehicles and shipments in real time."
    },
    {
        path: "predictions",
        title: "Predictive Intelligence",
        description: "AI-powered delay, breakdown, exception and demand predictions with confidence information."
    },
    {
        path: "network",
        title: "Network Disruption Intelligence",
        description: "Visualize logistics dependencies and plan disruption recovery actions."
    },
    {
        path: "whatsapp",
        title: "Qubriq Connect - WhatsApp Agent",
        description: "Manage WhatsApp integration, customer conversations, notification settings and delivery history."
    },
    {
        path: "finance",
        title: "Finance",
        description: "Finance workspace."
    },
    {
        path: "analytics",
        title: "Analytics",
        description: "Analytics workspace."
    },
    {
        path: "settings",
        title: "Settings",
        description: "Platform settings."
    }
];

function AppRoutes() {
    return (
        <Routes>
            <Route path="/" element={<LandingPage />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/signup" element={<SignupPage />} />

            <Route path="/app" element={<DashboardLayout />}>
                <Route index element={<Navigate to="overview" replace />} />
                <Route path="overview" element={<CommandCenterPage />} />
                <Route path="customers" element={<CustomersPage />} />

                {modules.map((module) => (
                    <Route
                        key={module.path}
                        path={module.path}
                        element={
                            <ModulePage
                                title={module.title}
                                description={module.description}
                            />
                        }
                    />
                ))}
            </Route>

            <Route path="/404" element={<NotFoundPage />} />
            <Route path="*" element={<Navigate to="/404" replace />} />
        </Routes>
    );
}

export default AppRoutes;

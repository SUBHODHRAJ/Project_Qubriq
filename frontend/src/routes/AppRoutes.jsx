import { Routes, Route, Navigate } from "react-router-dom";

import { useAuth } from "../context/AuthContext";
import { getRoleHome } from "./roleHome";
import ProtectedRoute from "./ProtectedRoute";

import LandingPage from "../pages/LandingPage";
import LoginPage from "../pages/LoginPage";
import SignupPage from "../pages/SignupPage";
import NotFoundPage from "../pages/NotFoundPage";
import UnauthorizedPage from "../pages/UnauthorizedPage";
import CustomerPortalPage from "../pages/CustomerPortalPage";
import RecipientPortalPage from "../pages/RecipientPortalPage";

import DashboardLayout from "../layouts/DashboardLayout";
import CommandCenterPage from "../pages/CommandCenterPage";
import CustomersPage from "../pages/CustomersPage";
import BookingsPage from "../pages/BookingsPage";
import ShipmentsPage from "../pages/ShipmentsPage";
import ShipmentDetailsPage from "../pages/ShipmentDetailsPage";
import DeliveryOperationsPage from "../pages/DeliveryOperationsPage";
import FleetPage from "../pages/FleetPage";
import LoadOptimizationPage from "../pages/LoadOptimizationPage";
import ModulePage from "../pages/ModulePage";
import AdminSettingsPage from "../pages/AdminSettingsPage";

const internalRoles = ["Admin", "OperationsDispatcher"];

const modules = [
    { path: "fleet", title: "Fleet & Capacity Planning", description: "Manage vehicles, driver assignments and shipment allocation.", component: FleetPage },

    { path: "compliance", title: "Compliance & E-Way Bill Management", description: "Manage shipment documents, E-Way Bill validity and compliance checks." },
    { path: "routes", title: "Route & Transportation Planning", description: "Plan trips, compare routes and check delivery feasibility and E-Way Bill validity." },
    { path: "tracking", title: "Live Tracking", description: "Track active vehicles and shipments in real time." },
    { path: "predictions", title: "Predictive Intelligence", description: "AI-powered delay, breakdown, exception and demand predictions." },
    { path: "network", title: "Network Disruption Intelligence", description: "Visualize logistics dependencies and plan disruption recovery actions." },
    { path: "whatsapp", title: "Qubriq Connect - WhatsApp Agent", description: "Manage WhatsApp integration and customer conversations." },
    { path: "analytics", title: "Analytics", description: "Analytics workspace." }
];

function HomeRedirect() {
    const { isAuthenticated, roles } = useAuth();

    if (!isAuthenticated) return <LandingPage />;

    return <Navigate to={getRoleHome(roles)} replace />;
}

function AppRoutes() {
    return (
        <Routes>
            <Route path="/" element={<HomeRedirect />} />
            <Route path="/login" element={<LoginPage />} />
            <Route path="/signup" element={<SignupPage />} />
            <Route path="/unauthorized" element={<UnauthorizedPage />} />

            <Route element={<ProtectedRoute allowedRoles={["BookingCustomer"]} />}>
                <Route path="/customer" element={<CustomerPortalPage />} />
                <Route path="/customer/bookings" element={<BookingsPage />} />
                <Route path="/customer/bookings/new" element={<BookingsPage />} />
            </Route>

            <Route element={<ProtectedRoute allowedRoles={["ShipmentRecipient"]} />}>
                <Route path="/recipient" element={<RecipientPortalPage />} />
            </Route>

            <Route element={<ProtectedRoute allowedRoles={internalRoles} />}>
                <Route path="/app" element={<DashboardLayout />}>
                    <Route index element={<Navigate to="overview" replace />} />
                    <Route path="overview" element={<CommandCenterPage />} />
                    <Route path="bookings" element={<BookingsPage />} />
                    <Route path="bookings/new" element={<BookingsPage />} />
                    <Route path="shipments" element={<ShipmentsPage />} />
                    <Route path="deliveries" element={<DeliveryOperationsPage />} />
                    <Route path="load-optimization" element={<LoadOptimizationPage />} />
                    <Route path="shipments/:id" element={<ShipmentDetailsPage />} />

                    <Route element={<ProtectedRoute allowedRoles={["Admin"]} />}>
                        <Route path="customers" element={<CustomersPage />} />
                        <Route path="finance" element={<ModulePage title="Finance" description="Finance workspace." />} />
                        <Route path="settings" element={<AdminSettingsPage />} />
                    </Route>

                    {modules.map(module => {
                        const PageComponent = module.component;

                        return (
                            <Route
                                key={module.path}
                                path={module.path}
                                element={
                                    PageComponent
                                        ? <PageComponent />
                                        : <ModulePage
                                            title={module.title}
                                            description={module.description}
                                        />
                                }
                            />
                        );
                    })}
                </Route>
            </Route>

            <Route path="/404" element={<NotFoundPage />} />
            <Route path="*" element={<Navigate to="/404" replace />} />
        </Routes>
    );
}

export default AppRoutes;








import { Navigate, Outlet, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function ProtectedRoute({ allowedRoles }) {
    const { isAuthenticated, roles } = useAuth();
    const location = useLocation();

    if (!isAuthenticated) {
        return <Navigate to="/login" replace state={{ from: location }} />;
    }

    if (allowedRoles && !allowedRoles.some(role => roles.includes(role))) {
        return <Navigate to="/unauthorized" replace />;
    }

    return <Outlet />;
}

export default ProtectedRoute;

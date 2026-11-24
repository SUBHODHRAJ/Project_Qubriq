import {
    createContext,
    useCallback,
    useContext,
    useMemo,
    useState
} from "react";

import { authService } from "../services/authService";
import { getAccessToken } from "../services/apiClient";

const AuthContext = createContext(null);

function readStoredUser() {
    try {
        const token = getAccessToken();
        const storedUser = sessionStorage.getItem("qubriq_user");
        const expiry = sessionStorage.getItem("qubriq_expires_at");

        if (!token || !storedUser) {
            return null;
        }

        if (expiry && new Date(expiry).getTime() <= Date.now()) {
            authService.logout();
            return null;
        }

        return JSON.parse(storedUser);
    } catch {
        authService.logout();
        return null;
    }
}

export function AuthProvider({ children }) {
    const [user, setUser] = useState(readStoredUser);

    const login = useCallback(async (email, password) => {
        const result = await authService.login(email, password);
        setUser(result.user);
        return result;
    }, []);

    const logout = useCallback(() => {
        authService.logout();
        setUser(null);
    }, []);

    const roles = useMemo(
        () => Array.isArray(user?.roles) ? user.roles : [],
        [user]
    );

    const hasRole = useCallback(
        (role) => roles.includes(role),
        [roles]
    );

    const value = useMemo(() => ({
        user,
        roles,
        isAuthenticated: Boolean(user && getAccessToken()),
        login,
        logout,
        hasRole
    }), [user, roles, login, logout, hasRole]);

    return (
        <AuthContext.Provider value={value}>
            {children}
        </AuthContext.Provider>
    );
}

export function useAuth() {
    const context = useContext(AuthContext);

    if (!context) {
        throw new Error("useAuth must be used inside AuthProvider.");
    }

    return context;
}

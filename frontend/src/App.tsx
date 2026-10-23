import { useEffect } from "react";
import { Navigate, Outlet, Route, Routes, useLocation } from "react-router-dom";
import { refreshSession } from "./api/authApi";
import DashboardLayout from "./components/DashboardLayout";
import LoginPage from "./pages/LoginPage";
import ProductsPage from "./pages/ProductsPage";
import ProductDetailsPage from "./pages/ProductDetailsPage";
import ProductFormPage from "./pages/ProductFormPage";
import SuppliersPage from "./pages/SuppliersPage";
import UsersPage from "./pages/UsersPage";
import ChangePasswordPage from "./pages/ChangePasswordPage";
import { useAuthStore } from "./stores/authStore";

function ProtectedLayout() {
  const location = useLocation();
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const mustChangePassword = useAuthStore((state) => state.mustChangePassword);
  const isInitialized = useAuthStore((state) => state.isInitialized);
  if (!isInitialized) return <InitializationScreen />;
  if (!isAuthenticated) return <Navigate to="/login" replace />;
  if (mustChangePassword && location.pathname !== "/change-password") return <Navigate to="/change-password" replace />;
  if (mustChangePassword) return <Outlet />;
  if (location.pathname === "/change-password") return <Navigate to="/products" replace />;
  return <DashboardLayout />;
}

function PublicOnly({ children }: { children: React.ReactNode }) {
  const isAuthenticated = useAuthStore((state) => state.isAuthenticated);
  const isInitialized = useAuthStore((state) => state.isInitialized);
  if (!isInitialized) return <InitializationScreen />;
  return isAuthenticated ? <Navigate to="/products" replace /> : children;
}

function AdminOnly({ children }: { children: React.ReactNode }) {
  const role = useAuthStore((state) => state.user?.role);
  return role?.toLowerCase() === "admin" ? children : <Navigate to="/products" replace />;
}

function InitializationScreen() {
  return <main className="grid min-h-screen place-items-center bg-[#F8FAF9] text-sm text-[#52645A]" role="status">Restoring your session…</main>;
}

let initialization: Promise<void> | undefined;

export default function App() {
  const isInitialized = useAuthStore((state) => state.isInitialized);
  const setSession = useAuthStore((state) => state.setSession);
  const finishInitialization = useAuthStore((state) => state.finishInitialization);

  useEffect(() => {
    if (isInitialized) return;
    initialization ??= refreshSession()
      .then(setSession)
      .catch(() => undefined)
      .then(finishInitialization)
      .finally(() => { initialization = undefined; });
  }, [finishInitialization, isInitialized, setSession]);

  return <Routes>
    <Route path="/login" element={<PublicOnly><LoginPage /></PublicOnly>} />
    <Route element={<ProtectedLayout />}>
      <Route path="/" element={<Navigate to="/products" replace />} />
      <Route path="/change-password" element={<ChangePasswordPage />} />
      <Route path="/products" element={<ProductsPage />} />
      <Route path="/products/new" element={<ProductFormPage />} />
      <Route path="/products/:productId" element={<ProductDetailsPage />} />
      <Route path="/products/:productId/edit" element={<ProductFormPage />} />
      <Route path="/suppliers" element={<SuppliersPage />} />
      <Route path="/users" element={<AdminOnly><UsersPage /></AdminOnly>} />
    </Route>
    <Route path="*" element={<Navigate to="/products" replace />} />
  </Routes>;
}

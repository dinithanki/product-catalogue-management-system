import { useEffect, useRef, useState } from "react";
import axios from "axios";
import { NavLink, Outlet, useNavigate } from "react-router-dom";
import { logoutSession } from "../api/authApi";
import { useAuthStore } from "../stores/authStore";
import ConfirmDialog from "./ConfirmDialog";

const linkClass = ({ isActive }: { isActive: boolean }) =>
  `flex items-center gap-3 rounded-lg px-3 py-2.5 text-sm font-medium transition ${isActive ? "bg-[#E6F4EA] text-[#245C43]" : "text-[#52645A] hover:bg-[#F2F7F3] hover:text-[#245C43]"}`;

export default function DashboardLayout() {
  const [collapsed, setCollapsed] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const [logoutDialogOpen, setLogoutDialogOpen] = useState(false);
  const [loggingOut, setLoggingOut] = useState(false);
  const menuButtonRef = useRef<HTMLButtonElement>(null);
  const closeButtonRef = useRef<HTMLButtonElement>(null);
  const navigate = useNavigate();
  const { user, clearSession } = useAuthStore();

  useEffect(() => {
    if (!drawerOpen) return;
    closeButtonRef.current?.focus();
    function handleKeyDown(event: KeyboardEvent) {
      if (event.key === "Escape") {
        setDrawerOpen(false);
        menuButtonRef.current?.focus();
      }
    }
    document.addEventListener("keydown", handleKeyDown);
    return () => document.removeEventListener("keydown", handleKeyDown);
  }, [drawerOpen]);

  async function handleLogout() {
    setLoggingOut(true);
    let logoutFailureStatus: number | null = null;
    let serverConfirmed = false;
    try {
      await logoutSession();
      serverConfirmed = true;
    } catch (error: unknown) {
      logoutFailureStatus = axios.isAxiosError(error) ? error.response?.status ?? null : null;
    }
    const logoutNotice = serverConfirmed
      ? "Signed out. The server revoked the refresh session."
      : logoutFailureStatus
        ? `Signed out locally. The server did not confirm revocation (HTTP ${logoutFailureStatus}).`
        : "Signed out locally. The server did not confirm revocation because it could not be reached.";
    clearSession();
    navigate("/login", { replace: true, state: { logoutNotice } });
  }

  const sidebar = (mobile = false) => (
    <div className="flex h-full flex-col bg-white">
      <div className={`flex items-center border-b border-[#DCE5DF] ${collapsed && !mobile ? "h-24 flex-col justify-center gap-1 px-2" : "h-16 justify-between px-4"}`}>
        <div className={`flex min-w-0 items-center gap-3 ${collapsed && !mobile ? "justify-center" : "overflow-hidden"}`}>
          <span className="grid h-9 w-9 shrink-0 place-items-center rounded-xl bg-[#438B68] font-bold text-white">K</span>
          {(!collapsed || mobile) && <span className="whitespace-nowrap font-semibold text-[#245C43]">Keells Catalogue</span>}
        </div>
        {mobile ? <button ref={closeButtonRef} aria-label="Close navigation" onClick={() => setDrawerOpen(false)} className="rounded p-2 text-[#64748B] hover:bg-[#F2F7F3]">×</button> : <button aria-label={collapsed ? "Expand sidebar" : "Collapse sidebar"} onClick={() => setCollapsed((value) => !value)} className="hidden rounded p-2 text-[#64748B] hover:bg-[#F2F7F3] lg:block">{collapsed ? "»" : "«"}</button>}
      </div>
      <nav aria-label="Main navigation" className="flex-1 space-y-1 p-3">
        <NavLink to="/products" onClick={() => setDrawerOpen(false)} className={linkClass}><span aria-hidden="true">▦</span>{(!collapsed || mobile) && "Products"}</NavLink>
        <NavLink to="/suppliers" onClick={() => setDrawerOpen(false)} className={linkClass}><span aria-hidden="true">⌂</span>{(!collapsed || mobile) && "Suppliers"}</NavLink>
        {user?.role.toLowerCase() === "admin" && <NavLink to="/users" onClick={() => setDrawerOpen(false)} className={linkClass}><span aria-hidden="true">♙</span>{(!collapsed || mobile) && "Users"}</NavLink>}
      </nav>
      <div className="border-t border-[#DCE5DF] p-3">
        {(!collapsed || mobile) && user && <div className="mb-3 truncate px-3 text-xs text-[#64748B]">Signed in as <strong className="text-[#24352B]">{user.username}</strong><br /><span>{user.role}</span></div>}
        <button onClick={() => setLogoutDialogOpen(true)} className="flex w-full items-center gap-3 rounded-lg px-3 py-2.5 text-left text-sm font-medium text-[#52645A] hover:bg-[#F2F7F3]"><span aria-hidden="true">↪</span>{(!collapsed || mobile) && "Log out"}</button>
      </div>
    </div>
  );

  return (
    <div className="min-h-screen bg-[#F8FAF9] text-[#24352B]">
      <aside className={`fixed inset-y-0 left-0 z-30 hidden border-r border-[#DCE5DF] transition-[width] duration-200 lg:block ${collapsed ? "w-[76px]" : "w-64"}`}>{sidebar()}</aside>
      {drawerOpen && <div className="fixed inset-0 z-40 lg:hidden"><button aria-label="Close menu backdrop" className="absolute inset-0 bg-[#24352B]/20" onClick={() => setDrawerOpen(false)} /><aside id="mobile-navigation" className="absolute inset-y-0 left-0 w-72 shadow-xl">{sidebar(true)}</aside></div>}
      <ConfirmDialog open={logoutDialogOpen} title="Log out?" message="You will be signed out of the Keells Catalogue on this device." confirmLabel="Log out" busy={loggingOut} onClose={() => setLogoutDialogOpen(false)} onConfirm={() => void handleLogout()} />
      <div className={`min-h-screen min-w-0 transition-[margin] duration-200 ${collapsed ? "lg:ml-[76px]" : "lg:ml-64"}`}>
        <header className="sticky top-0 z-20 flex h-16 items-center justify-between border-b border-[#DCE5DF] bg-white/95 px-4 backdrop-blur sm:px-6">
          <button ref={menuButtonRef} onClick={() => setDrawerOpen(true)} aria-expanded={drawerOpen} aria-controls="mobile-navigation" aria-label="Open navigation menu" className="rounded-lg border border-[#DCE5DF] px-3 py-2 text-sm text-[#245C43] lg:hidden">☰</button>
          <span className="hidden text-sm font-semibold text-[#245C43] sm:block">Product Catalogue</span>
          <div className="ml-auto min-w-0 max-w-[65%] text-right"><p className="truncate text-sm font-semibold">{user?.username ?? "User"}</p><p className="truncate text-xs text-[#64748B]">{user?.role ?? ""}</p></div>
        </header>
        <main className="mx-auto w-full max-w-[1440px] min-w-0 px-3 py-4 sm:p-6 lg:p-8"><Outlet /></main>
      </div>
    </div>
  );
}

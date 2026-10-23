import { useCallback, useEffect, useState, type FormEvent } from "react";
import axios from "axios";
import { activateUser, createUser, deactivateUser, getUsers, resetUserPassword, updateUser, type ManagedUser, type UserRequest } from "../api/userApi";
import { useAuthStore } from "../stores/authStore";

const inputClass = "w-full rounded-lg border border-[#DCE5DF] bg-white px-3 py-2.5 text-sm text-[#24352B] outline-none focus:border-[#438B68] focus:ring-4 focus:ring-[#E6F4EA]";
const initial: UserRequest = { username: "", role: "User" };

function errorMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: unknown } | undefined;
    if (typeof data?.message === "string") return data.message;
    if (error.response?.status === 403) return "Only administrators can manage user accounts.";
    if (!error.response) return "Cannot connect to the server.";
    return `${fallback} (HTTP ${error.response.status}).`;
  }
  return fallback;
}

export default function UsersPage() {
  const user = useAuthStore((state) => state.user);
  const [users, setUsers] = useState<ManagedUser[]>([]);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | "active" | "inactive">("all");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<ManagedUser | null>(null);
  const [form, setForm] = useState<UserRequest>(initial);
  const [formError, setFormError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const [busyUserId, setBusyUserId] = useState<number | null>(null);
  const [temporaryPassword, setTemporaryPassword] = useState<{ username: string; password: string } | null>(null);
  const filteredUsers = users.filter((item) =>
    (statusFilter === "all" || item.isActive === (statusFilter === "active")) &&
    [item.username, item.role, item.isActive ? "active" : "inactive", item.mustChangePassword ? "change required" : "current"]
      .some((value) => value.toLowerCase().includes(search.trim().toLowerCase())),
  );

  useEffect(() => {
    if (!successMessage) return;
    const timer = window.setTimeout(() => setSuccessMessage(""), 5000);
    return () => window.clearTimeout(timer);
  }, [successMessage]);

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try { setUsers(await getUsers()); }
    catch (requestError) { setError(errorMessage(requestError, "Could not load users. Please retry.")); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  function openCreate() {
    setEditing(null); setForm(initial); setFormError(""); setFormOpen((open) => !open);
  }

  function openEdit(item: ManagedUser) {
    setEditing(item); setForm({ username: item.username, role: item.role }); setFormError(""); setFormOpen(true);
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setFormError(""); setSuccessMessage("");
    const username = form.username.trim();
    if (username.length < 3 || username.length > 50) { setFormError("Username must be between 3 and 50 characters."); return; }
    setSaving(true);
    try {
      if (editing) {
        await updateUser(editing.userId, { username, role: form.role });
        setTemporaryPassword(null);
        setSuccessMessage("User updated successfully.");
      } else {
        const result = await createUser({ username, role: form.role });
        setTemporaryPassword({ username, password: result.temporaryPassword });
        setSuccessMessage("User created successfully.");
      }
      setForm(initial); setEditing(null); setFormOpen(false); await load();
    } catch (requestError) { setFormError(errorMessage(requestError, `Could not ${editing ? "update" : "create"} user. Please retry.`)); }
    finally { setSaving(false); }
  }

  async function resetPassword(item: ManagedUser) {
    setBusyUserId(item.userId); setError(""); setSuccessMessage(""); setTemporaryPassword(null);
    try {
      const result = await resetUserPassword(item.userId);
      setTemporaryPassword({ username: item.username, password: result.temporaryPassword });
      setSuccessMessage(`Password reset successfully for ${item.username}.`);
      await load();
    } catch (requestError) { setError(errorMessage(requestError, "Could not reset password. Please retry.")); }
    finally { setBusyUserId(null); }
  }

  async function toggleStatus(item: ManagedUser) {
    setBusyUserId(item.userId); setError(""); setSuccessMessage("");
    try {
      if (item.isActive) await deactivateUser(item.userId);
      else await activateUser(item.userId);
      setSuccessMessage(item.isActive ? "User deactivated successfully." : "User activated successfully.");
      await load();
    } catch (requestError) { setError(errorMessage(requestError, "Could not update account status. Please retry.")); }
    finally { setBusyUserId(null); }
  }

  return <div>
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div><p className="text-sm font-semibold text-[#438B68]">ADMINISTRATION</p><h1 className="mt-1 text-2xl font-bold text-[#245C43]">Users</h1><p className="mt-1 text-sm text-[#64748B]">Create accounts, manage roles and status, and issue password resets.</p></div>
      <button onClick={openCreate} className="rounded-lg bg-[#438B68] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#367354]">{formOpen && !editing ? "Close form" : "Add user"}</button>
    </div>

    {successMessage && <p role="status" className="mb-4 rounded-lg border border-[#B8D8C1] bg-[#E6F4EA] p-3 text-sm text-[#245C43]">{successMessage}</p>}

    {temporaryPassword && <section role="status" className="mb-5 rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-950"><div className="flex flex-wrap items-start justify-between gap-3"><div><h2 className="font-semibold">Temporary password for {temporaryPassword.username}</h2><p className="mt-1">Share this password securely. It is shown only now, and the user must change it at next sign in.</p><code className="mt-3 inline-block rounded bg-white px-3 py-2 font-mono text-base">{temporaryPassword.password}</code></div><button onClick={() => setTemporaryPassword(null)} className="font-semibold underline">Dismiss</button></div></section>}

    {formOpen && <form onSubmit={(event) => void submit(event)} className="mb-6 rounded-xl border border-[#DCE5DF] bg-white p-5 shadow-sm sm:p-6"><div className="flex items-center justify-between"><h2 className="text-lg font-semibold text-[#245C43]">{editing ? `Edit ${editing.username}` : "New user"}</h2><button type="button" onClick={() => { setFormOpen(false); setEditing(null); }} className="text-sm font-semibold text-[#52645A]">Close</button></div>{formError && <p role="alert" className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800">{formError}</p>}<div className="mt-4 grid gap-4 sm:grid-cols-2"><label className="grid gap-1.5 text-sm font-medium">Username<input required minLength={3} maxLength={50} className={inputClass} value={form.username} onChange={(event) => setForm((current) => ({ ...current, username: event.target.value }))} /></label><label className="grid gap-1.5 text-sm font-medium">Role<select className={inputClass} value={form.role} onChange={(event) => setForm((current) => ({ ...current, role: event.target.value as UserRequest["role"] }))}><option value="User">User</option><option value="Admin">Admin</option></select></label></div><div className="mt-5 flex justify-end"><button disabled={saving} className="rounded-lg bg-[#438B68] px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{saving ? "Saving…" : editing ? "Save changes" : "Create user"}</button></div></form>}

    {error && <div role="alert" className="mb-4 flex items-center justify-between rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800"><span>{error}</span><button onClick={() => void load()} className="font-semibold underline">Retry</button></div>}
    <section aria-label="User filters" className="mb-4 grid gap-3 rounded-xl border border-[#DCE5DF] bg-white p-4 shadow-sm sm:grid-cols-2"><label className="grid gap-1.5 text-xs font-semibold text-[#52645A]">Search users<input className={inputClass} type="search" maxLength={100} value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Username, role or status" /></label><label className="grid gap-1.5 text-xs font-semibold text-[#52645A]">Status<select className={inputClass} value={statusFilter} onChange={(event) => setStatusFilter(event.target.value as typeof statusFilter)}><option value="all">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select></label></section>
    <section className="overflow-hidden rounded-xl border border-[#DCE5DF] bg-white shadow-sm"><div className="border-b border-[#DCE5DF] px-4 py-3 text-sm text-[#64748B]">{filteredUsers.length} users</div><div className="overflow-x-auto"><table className="w-full min-w-[800px] text-left text-sm"><thead className="bg-[#F2F7F3]"><tr>{["Username", "Role", "Account status", "Password status", "Actions"].map((label) => <th key={label} scope="col" className="px-4 py-3 text-xs font-semibold uppercase tracking-wide text-[#64748B]">{label}</th>)}</tr></thead><tbody className="divide-y divide-[#EDF1EE]">{loading ? <tr><td colSpan={5} className="p-12 text-center text-[#64748B]">Loading users…</td></tr> : filteredUsers.length === 0 ? <tr><td colSpan={5} className="p-12 text-center text-[#64748B]">{search || statusFilter !== "all" ? "No users match these filters." : "No users found."}</td></tr> : filteredUsers.map((item) => {
      const isSelf = item.userId === user?.userId;
      const busy = busyUserId === item.userId;
      return <tr key={item.userId} className={item.isActive ? "" : "bg-red-50/60"}><td className="px-4 py-4 font-semibold text-[#24352B]">{item.username}{isSelf && <span className="ml-2 text-xs font-normal text-[#64748B]">You</span>}</td><td className="px-4 py-4">{item.role}</td><td className="px-4 py-4"><span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${item.isActive ? "bg-[#E6F4EA] text-[#245C43]" : "bg-slate-100 text-slate-600"}`}>{item.isActive ? "Active" : "Inactive"}</span></td><td className="px-4 py-4">{item.mustChangePassword ? "Change required" : "Current"}</td><td className="px-4 py-4"><div className="flex flex-wrap gap-x-3 gap-y-1"><button onClick={() => openEdit(item)} className="font-semibold text-[#438B68] hover:underline">Edit</button><button disabled={busy} onClick={() => void resetPassword(item)} className="font-semibold text-[#438B68] hover:underline disabled:opacity-50">{busy ? "Saving…" : "Reset password"}</button>{!isSelf && <button disabled={busy} onClick={() => void toggleStatus(item)} className="font-semibold text-[#52645A] hover:underline disabled:opacity-50">{item.isActive ? "Deactivate" : "Activate"}</button>}</div></td></tr>;
    })}</tbody></table></div></section>
  </div>;
}

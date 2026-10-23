import { useCallback, useEffect, useState, type FormEvent } from "react";
import axios from "axios";
import { createSupplier, deactivateSupplier, getSuppliers, reactivateSupplier, updateSupplier, type Supplier, type SupplierRequest } from "../api/supplierApi";
import { useAuthStore } from "../stores/authStore";

const inputClass = "w-full rounded-lg border border-[#DCE5DF] bg-white px-3 py-2.5 text-sm text-[#24352B] outline-none focus:border-[#438B68] focus:ring-4 focus:ring-[#E6F4EA]";
const initial: SupplierRequest = { name: "", country: "", currencyCode: "", contactNumber: "", email: "" };
const formFields: { key: keyof SupplierRequest; label: string; type?: string; maxLength: number }[] = [
  { key: "name", label: "Supplier name", maxLength: 150 },
  { key: "country", label: "Country", maxLength: 100 },
  { key: "currencyCode", label: "Currency code", maxLength: 3 },
  { key: "contactNumber", label: "Contact number", maxLength: 30 },
  { key: "email", label: "Email", type: "email", maxLength: 254 },
];

function supplierRequest(supplier: Supplier): SupplierRequest {
  return { name: supplier.name, country: supplier.country, currencyCode: supplier.currencyCode, contactNumber: supplier.contactNumber, email: supplier.email };
}

function apiMessage(error: unknown, fallback: string) {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: unknown } | undefined;
    if (typeof data?.message === "string") return data.message;
    if (error.response?.status === 403) return "Only administrators can manage suppliers.";
    if (!error.response) return "Cannot connect to the server.";
    return `${fallback} (HTTP ${error.response.status}).`;
  }
  return fallback;
}

export default function SuppliersPage() {
  const user = useAuthStore((state) => state.user);
  const isAdmin = user?.role.toLowerCase() === "admin";
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [search, setSearch] = useState("");
  const [statusFilter, setStatusFilter] = useState<"all" | "active" | "inactive">("all");
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [editing, setEditing] = useState<Supplier | null>(null);
  const [form, setForm] = useState(initial);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState("");
  const [successMessage, setSuccessMessage] = useState("");
  const [saving, setSaving] = useState(false);
  const [busySupplierId, setBusySupplierId] = useState<number | null>(null);
  const filteredSuppliers = suppliers.filter((supplier) =>
    (statusFilter === "all" || supplier.isActive === (statusFilter === "active")) &&
    [supplier.name, supplier.country, supplier.currencyCode, supplier.contactNumber, supplier.email]
      .some((value) => value.toLowerCase().includes(search.trim().toLowerCase())),
  );

  useEffect(() => {
    if (!successMessage) return;
    const timer = window.setTimeout(() => setSuccessMessage(""), 5000);
    return () => window.clearTimeout(timer);
  }, [successMessage]);

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try { setSuppliers(await getSuppliers()); }
    catch (requestError) { setError(apiMessage(requestError, "Could not load suppliers. Please retry.")); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  function startCreate() {
    setEditing(null); setForm(initial); setFieldErrors({}); setFormError(""); setFormOpen((open) => !open);
  }

  function startEdit(supplier: Supplier) {
    setEditing(supplier); setForm(supplierRequest(supplier)); setFieldErrors({}); setFormError(""); setFormOpen(true);
  }

  function validate() {
    const errors: Record<string, string> = {};
    if (!form.name.trim() || form.name.trim().length > 150) errors.name = "Name is required (150 characters maximum).";
    if (!form.country.trim() || form.country.trim().length > 100) errors.country = "Country is required (100 characters maximum).";
    if (!/^[A-Za-z]{3}$/.test(form.currencyCode.trim())) errors.currencyCode = "Enter a three-letter currency code.";
    if (!form.contactNumber.trim() || form.contactNumber.trim().length > 30) errors.contactNumber = "Contact number is required (30 characters maximum).";
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim()) || form.email.trim().length > 254) errors.email = "Enter a valid email address.";
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setFormError(""); setSuccessMessage("");
    if (!validate()) return;
    const payload = { ...form, name: form.name.trim(), country: form.country.trim(), currencyCode: form.currencyCode.trim().toUpperCase(), contactNumber: form.contactNumber.trim(), email: form.email.trim() };
    setSaving(true);
    try {
      const wasEditing = Boolean(editing);
      if (editing) await updateSupplier(editing.supplierId, payload);
      else await createSupplier(payload);
      setSuccessMessage(wasEditing ? "Supplier updated successfully." : "Supplier created successfully.");
      setForm(initial); setEditing(null); setFormOpen(false); await load();
    } catch (requestError) {
      if (axios.isAxiosError(requestError)) {
        const data = requestError.response?.data as { errors?: Record<string, string[]> } | undefined;
        if (data?.errors) setFieldErrors(Object.fromEntries(Object.entries(data.errors).map(([key, values]) => [key.charAt(0).toLowerCase() + key.slice(1), values[0] ?? "Invalid value."])));
      }
      setFormError(apiMessage(requestError, `Could not ${editing ? "update" : "create"} supplier. Please retry.`));
    } finally { setSaving(false); }
  }

  async function toggleStatus(supplier: Supplier) {
    setBusySupplierId(supplier.supplierId); setError(""); setSuccessMessage("");
    try {
      if (supplier.isActive) await deactivateSupplier(supplier.supplierId);
      else await reactivateSupplier(supplier.supplierId);
      setSuccessMessage(supplier.isActive ? "Supplier deactivated successfully." : "Supplier reactivated successfully.");
      await load();
    } catch (requestError) { setError(apiMessage(requestError, "Could not update supplier status. Please retry.")); }
    finally { setBusySupplierId(null); }
  }

  return <div>
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div><p className="text-sm font-semibold text-[#438B68]">PARTNERS</p><h1 className="mt-1 text-2xl font-bold text-[#245C43]">Suppliers</h1><p className="mt-1 text-sm text-[#64748B]">Supplier contacts and supported currencies.</p></div>
      {isAdmin && <button onClick={startCreate} className="rounded-lg bg-[#438B68] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#367354]">{formOpen && !editing ? "Close form" : "Add supplier"}</button>}
    </div>
    {successMessage && <p role="status" className="mb-4 rounded-lg border border-[#B8D8C1] bg-[#E6F4EA] p-3 text-sm text-[#245C43]">{successMessage}</p>}
    {formOpen && isAdmin && <form onSubmit={(event) => void submit(event)} className="mb-6 rounded-xl border border-[#DCE5DF] bg-white p-5 shadow-sm sm:p-6" noValidate>
      <div className="flex items-center justify-between"><h2 className="text-lg font-semibold text-[#245C43]">{editing ? `Edit ${editing.name}` : "New supplier"}</h2><button type="button" onClick={() => { setFormOpen(false); setEditing(null); }} className="text-sm font-semibold text-[#52645A]">Close</button></div>
      {formError && <p role="alert" className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800">{formError}</p>}
      <div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{formFields.map((field) => <label key={field.key} className="grid gap-1.5 text-sm font-medium text-[#24352B]">{field.label}<input className={inputClass} type={field.type ?? "text"} maxLength={field.maxLength} value={form[field.key]} onChange={(event) => { setForm((current) => ({ ...current, [field.key]: event.target.value })); setFieldErrors((current) => ({ ...current, [field.key]: "" })); }} aria-invalid={Boolean(fieldErrors[field.key])} />{fieldErrors[field.key] && <span className="text-xs font-normal text-red-700">{fieldErrors[field.key]}</span>}</label>)}</div>
      <div className="mt-5 flex justify-end"><button disabled={saving} className="rounded-lg bg-[#438B68] px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{saving ? "Saving…" : editing ? "Save changes" : "Create supplier"}</button></div>
    </form>}
    {error && <div role="alert" className="mb-4 flex items-center justify-between rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800"><span>{error}</span><button onClick={() => void load()} className="font-semibold underline">Retry</button></div>}
    <section aria-label="Supplier filters" className="mb-4 grid gap-3 rounded-xl border border-[#DCE5DF] bg-white p-4 shadow-sm sm:grid-cols-2">
      <label className="grid max-w-xl gap-1.5 text-xs font-semibold text-[#52645A]">Search suppliers<input className={inputClass} type="search" maxLength={254} value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Name, country, currency, contact or email" /></label>
      <label className="grid gap-1.5 text-xs font-semibold text-[#52645A]">Status<select className={inputClass} value={statusFilter} onChange={(event) => setStatusFilter(event.target.value as typeof statusFilter)}><option value="all">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select></label>
    </section>
    <section className="overflow-hidden rounded-xl border border-[#DCE5DF] bg-white shadow-sm">
      <div className="border-b border-[#DCE5DF] px-4 py-3 text-sm text-[#64748B]">{filteredSuppliers.length} suppliers</div>
      <div className="overflow-x-auto"><table className="w-full min-w-[880px] text-left text-sm"><thead className="bg-[#F2F7F3]"><tr>{["Supplier", "Country", "Currency", "Contact", "Email", "Status", ...(isAdmin ? ["Actions"] : [])].map((label) => <th key={label} scope="col" className="px-4 py-3 text-xs font-semibold uppercase tracking-wide text-[#64748B]">{label}</th>)}</tr></thead>
        <tbody className="divide-y divide-[#EDF1EE]">{loading ? <tr><td colSpan={isAdmin ? 7 : 6} className="p-12 text-center text-[#64748B]">Loading suppliers…</td></tr> : filteredSuppliers.length === 0 ? <tr><td colSpan={isAdmin ? 7 : 6} className="p-12 text-center text-[#64748B]">{search || statusFilter !== "all" ? "No suppliers match these filters." : "No suppliers found."}</td></tr> : filteredSuppliers.map((supplier) => <tr key={supplier.supplierId}>
          <td className="px-4 py-4 font-semibold text-[#24352B]">{supplier.name}</td><td className="px-4 py-4">{supplier.country}</td><td className="px-4 py-4">{supplier.currencyCode}</td><td className="px-4 py-4">{supplier.contactNumber}</td><td className="px-4 py-4">{supplier.email}</td>
          <td className="px-4 py-4"><span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${supplier.isActive ? "bg-[#E6F4EA] text-[#245C43]" : "bg-slate-100 text-slate-600"}`}>{supplier.isActive ? "Active" : "Inactive"}</span></td>
          {isAdmin && <td className="px-4 py-4"><div className="flex gap-3"><button onClick={() => startEdit(supplier)} className="font-semibold text-[#438B68] hover:underline">Edit</button><button disabled={busySupplierId === supplier.supplierId} onClick={() => void toggleStatus(supplier)} className="font-semibold text-[#52645A] hover:underline disabled:opacity-50">{busySupplierId === supplier.supplierId ? "Saving…" : supplier.isActive ? "Deactivate" : "Reactivate"}</button></div></td>}
        </tr>)}</tbody></table></div>
    </section>
  </div>;
}

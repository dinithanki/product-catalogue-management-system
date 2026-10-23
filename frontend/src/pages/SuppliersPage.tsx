import { useCallback, useEffect, useState, type FormEvent } from "react";
import axios from "axios";
import { createSupplier, getSuppliers, type Supplier, type SupplierRequest } from "../api/supplierApi";
import { useAuthStore } from "../stores/authStore";

const inputClass = "w-full rounded-lg border border-[#DCE5DF] bg-white px-3 py-2.5 text-sm text-[#24352B] outline-none focus:border-[#438B68] focus:ring-4 focus:ring-[#E6F4EA]";
const initial: SupplierRequest = { name: "", country: "", currencyCode: "", contactNumber: "", email: "" };

export default function SuppliersPage() {
  const user = useAuthStore((state) => state.user);
  const isAdmin = user?.role.toLowerCase() === "admin";
  const [suppliers, setSuppliers] = useState<Supplier[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const [formOpen, setFormOpen] = useState(false);
  const [form, setForm] = useState(initial);
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState("");
  const [saving, setSaving] = useState(false);

  const load = useCallback(async () => {
    setLoading(true); setError("");
    try { setSuppliers(await getSuppliers()); }
    catch (requestError) {
      if (axios.isAxiosError(requestError) && requestError.response?.status === 403) setError("You do not have permission to view suppliers.");
      else if (axios.isAxiosError(requestError) && !requestError.response) setError("Cannot connect to the server.");
      else setError("Could not load suppliers. Please retry.");
    } finally { setLoading(false); }
  }, []);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setFormError("");
    const errors: Record<string, string> = {};
    if (!form.name.trim() || form.name.trim().length > 150) errors.name = "Name is required (150 characters maximum).";
    if (!form.country.trim() || form.country.trim().length > 100) errors.country = "Country is required (100 characters maximum).";
    if (!/^[A-Za-z]{3}$/.test(form.currencyCode.trim())) errors.currencyCode = "Enter a three-letter currency code.";
    if (!form.contactNumber.trim() || form.contactNumber.trim().length > 30) errors.contactNumber = "Contact number is required (30 characters maximum).";
    if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email.trim()) || form.email.trim().length > 254) errors.email = "Enter a valid email address.";
    setFieldErrors(errors);
    if (Object.keys(errors).length) return;
    setSaving(true);
    try {
      await createSupplier({ ...form, name: form.name.trim(), country: form.country.trim(), currencyCode: form.currencyCode.trim().toUpperCase(), contactNumber: form.contactNumber.trim(), email: form.email.trim() });
      setForm(initial); setFormOpen(false); await load();
    } catch (requestError) {
      if (axios.isAxiosError(requestError)) {
        const data = requestError.response?.data as { message?: unknown; errors?: Record<string, string[]> } | undefined;
        if (data?.errors) setFieldErrors(Object.fromEntries(Object.entries(data.errors).map(([key, values]) => [key.charAt(0).toLowerCase() + key.slice(1), values[0] ?? "Invalid value."])));
        setFormError(typeof data?.message === "string" ? data.message : requestError.response?.status === 403 ? "Only administrators can create suppliers." : !requestError.response ? "Cannot connect to the server." : `Could not create supplier (HTTP ${requestError.response.status}).`);
      } else setFormError("Could not create supplier. Please retry.");
    } finally { setSaving(false); }
  }

  const formFields: { key: keyof SupplierRequest; label: string; type?: string; maxLength: number }[] = [
    { key: "name", label: "Supplier name", maxLength: 150 },
    { key: "country", label: "Country", maxLength: 100 },
    { key: "currencyCode", label: "Currency code", maxLength: 3 },
    { key: "contactNumber", label: "Contact number", maxLength: 30 },
    { key: "email", label: "Email", type: "email", maxLength: 254 },
  ];

  return <div>
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between"><div><p className="text-sm font-semibold text-[#438B68]">PARTNERS</p><h1 className="mt-1 text-2xl font-bold text-[#245C43]">Suppliers</h1><p className="mt-1 text-sm text-[#64748B]">Supplier contacts and supported currencies.</p></div>{isAdmin && <button onClick={() => setFormOpen((open) => !open)} className="rounded-lg bg-[#438B68] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#367354]">{formOpen ? "Close form" : "Add supplier"}</button>}</div>
    {formOpen && isAdmin && <form onSubmit={(event) => void submit(event)} className="mb-6 rounded-xl border border-[#DCE5DF] bg-white p-5 shadow-sm sm:p-6" noValidate><h2 className="text-lg font-semibold text-[#245C43]">New supplier</h2>{formError && <p role="alert" className="mt-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800">{formError}</p>}<div className="mt-4 grid gap-4 sm:grid-cols-2 lg:grid-cols-3">{formFields.map((field) => <label key={field.key} className="grid gap-1.5 text-sm font-medium text-[#24352B]">{field.label}<input className={inputClass} type={field.type ?? "text"} maxLength={field.maxLength} value={form[field.key]} onChange={(event) => { setForm((current) => ({ ...current, [field.key]: event.target.value })); setFieldErrors((current) => ({ ...current, [field.key]: "" })); }} aria-invalid={Boolean(fieldErrors[field.key])} />{fieldErrors[field.key] && <span className="text-xs font-normal text-red-700">{fieldErrors[field.key]}</span>}</label>)}</div><div className="mt-5 flex justify-end"><button disabled={saving} className="rounded-lg bg-[#438B68] px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50">{saving ? "Saving…" : "Create supplier"}</button></div></form>}
    {error && <div role="alert" className="mb-4 flex items-center justify-between rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800"><span>{error}</span><button onClick={() => void load()} className="font-semibold underline">Retry</button></div>}
    <section className="overflow-hidden rounded-xl border border-[#DCE5DF] bg-white shadow-sm"><div className="border-b border-[#DCE5DF] px-4 py-3 text-sm text-[#64748B]">{suppliers.length} suppliers</div><div className="overflow-x-auto"><table className="w-full min-w-[760px] text-left text-sm"><thead className="bg-[#F2F7F3]"><tr>{["Supplier", "Country", "Currency", "Contact", "Email", "Status"].map((label) => <th key={label} scope="col" className="px-4 py-3 text-xs font-semibold uppercase tracking-wide text-[#64748B]">{label}</th>)}</tr></thead><tbody className="divide-y divide-[#EDF1EE]">{loading ? <tr><td colSpan={6} className="p-12 text-center text-[#64748B]">Loading suppliers…</td></tr> : suppliers.length === 0 ? <tr><td colSpan={6} className="p-12 text-center text-[#64748B]">No suppliers found.</td></tr> : suppliers.map((supplier) => <tr key={supplier.supplierId}><td className="px-4 py-4 font-semibold text-[#24352B]">{supplier.name}</td><td className="px-4 py-4">{supplier.country}</td><td className="px-4 py-4">{supplier.currencyCode}</td><td className="px-4 py-4">{supplier.contactNumber}</td><td className="px-4 py-4">{supplier.email}</td><td className="px-4 py-4"><span className={`rounded-full px-2.5 py-1 text-xs font-semibold ${supplier.isActive ? "bg-[#E6F4EA] text-[#245C43]" : "bg-slate-100 text-slate-600"}`}>{supplier.isActive ? "Active" : "Inactive"}</span></td></tr>)}</tbody></table></div></section>
  </div>;
}

import { useEffect, useState, type FormEvent } from "react";
import axios from "axios";
import { Link, useLocation, useNavigate, useParams } from "react-router-dom";
import { createProduct, getProduct, updateProduct, type ProductRequest } from "../api/productApi";
import { getActiveSuppliers, type ActiveSupplier } from "../api/supplierApi";

interface FormValues {
  sku: string;
  name: string;
  category: string;
  supplierId: string;
  unitPrice: string;
  packSize: string;
}

const blank: FormValues = { sku: "", name: "", category: "", supplierId: "", unitPrice: "", packSize: "" };
const inputClass = "w-full rounded-lg border border-[#DCE5DF] bg-white px-3 py-2.5 text-sm text-[#24352B] outline-none focus:border-[#438B68] focus:ring-4 focus:ring-[#E6F4EA]";

export default function ProductFormPage() {
  const { productId } = useParams();
  const location = useLocation();
  const navigate = useNavigate();
  const editing = Boolean(productId);
  const id = Number(productId);
  const returnTo = editing && (location.state as { returnTo?: unknown } | null)?.returnTo === `/products/${productId}`
    ? `/products/${productId}`
    : "/products";
  const [values, setValues] = useState<FormValues>(blank);
  const [suppliers, setSuppliers] = useState<ActiveSupplier[]>([]);
  const [loading, setLoading] = useState(editing);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState("");
  const [fieldErrors, setFieldErrors] = useState<Record<string, string>>({});
  const [success, setSuccess] = useState("");
  const [reloadVersion, setReloadVersion] = useState(0);

  useEffect(() => {
    let active = true;
    async function load() {
      try {
        const supplierRows = await getActiveSuppliers();
        if (active) setSuppliers(supplierRows);
        if (editing) {
          if (!Number.isInteger(id) || id < 1) throw new Error("Invalid product ID.");
          const product = await getProduct(id);
          if (active) setValues({ sku: product.sku, name: product.name, category: product.category, supplierId: String(product.supplierId), unitPrice: String(product.unitPrice), packSize: product.packSize });
        }
      } catch (requestError) {
        if (active) setError(requestError instanceof Error && !axios.isAxiosError(requestError) ? requestError.message : axios.isAxiosError(requestError) && requestError.response?.status === 404 ? "Product not found." : "Could not load the form data. Please retry.");
      } finally { if (active) setLoading(false); }
    }
    void load();
    return () => { active = false; };
  }, [editing, id, reloadVersion]);

  function updateField(field: keyof FormValues, value: string) {
    setValues((current) => ({ ...current, [field]: value }));
    setFieldErrors((current) => ({ ...current, [field]: "" }));
  }

  function validate(): boolean {
    const errors: Record<string, string> = {};
    if (!values.sku.trim()) errors.sku = "SKU is required.";
    else if (values.sku.trim().length > 50 || !/^[A-Za-z0-9_-]+$/.test(values.sku.trim())) errors.sku = "Use up to 50 letters, numbers, hyphens, or underscores.";
    if (!values.name.trim() || values.name.trim().length > 150) errors.name = "Enter a product name up to 150 characters.";
    if (!values.category.trim() || values.category.trim().length > 100) errors.category = "Enter a category up to 100 characters.";
    if (!values.supplierId || Number(values.supplierId) < 1) errors.supplierId = "Select an active supplier.";
    const price = Number(values.unitPrice);
    if (!values.unitPrice || !Number.isFinite(price) || price < 0 || !/^\d{1,14}(?:\.\d{1,4})?$/.test(values.unitPrice)) errors.unitPrice = "Enter a non-negative price with up to 14 digits and 4 decimal places.";
    if (!values.packSize.trim() || values.packSize.trim().length > 50) errors.packSize = "Enter a pack size up to 50 characters.";
    setFieldErrors(errors);
    return Object.keys(errors).length === 0;
  }

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault(); setError(""); setSuccess("");
    if (!validate()) return;
    const request: ProductRequest = { sku: values.sku.trim(), name: values.name.trim(), category: values.category.trim(), supplierId: Number(values.supplierId), unitPrice: Number(values.unitPrice), packSize: values.packSize.trim() };
    setSaving(true);
    try {
      if (editing) { await updateProduct(id, request); navigate(returnTo, { replace: true, state: { successMessage: "Product updated successfully." } }); return; }
      else { const result = await createProduct(request); navigate(`/products/${result.productId}`, { replace: true, state: { successMessage: "Product created successfully." } }); return; }
    } catch (requestError) {
      if (axios.isAxiosError(requestError)) {
        const data = requestError.response?.data as { message?: unknown; errors?: Record<string, string[]> } | undefined;
        if (data?.errors) setFieldErrors(Object.fromEntries(Object.entries(data.errors).map(([key, messages]) => [key.charAt(0).toLowerCase() + key.slice(1), messages[0] ?? "Invalid value."])));
        setError(typeof data?.message === "string" ? data.message : requestError.response?.status === 403 ? "You do not have permission to save this product." : !requestError.response ? "Cannot connect to the server." : `Could not save the product (HTTP ${requestError.response.status}).`);
      } else setError("Could not save the product. Please retry.");
    } finally { setSaving(false); }
  }

  if (loading) return <p role="status" className="rounded-xl border border-[#DCE5DF] bg-white p-8 text-center text-[#64748B]">Loading form…</p>;
  const fields: { key: keyof FormValues; label: string; maxLength: number; type?: string }[] = [
    { key: "sku", label: "SKU", maxLength: 50 }, { key: "name", label: "Product name", maxLength: 150 }, { key: "category", label: "Category", maxLength: 100 }, { key: "unitPrice", label: "Unit price", maxLength: 24, type: "number" }, { key: "packSize", label: "Pack size", maxLength: 50 },
  ];

  return <div className="mx-auto max-w-3xl"><Link to={returnTo} className="text-sm font-semibold text-[#438B68] hover:underline">← Back</Link><div className="mb-6 mt-3"><p className="text-sm font-semibold text-[#438B68]">PRODUCTS</p><h1 className="mt-1 text-2xl font-bold text-[#245C43]">{editing ? "Edit product" : "Add product"}</h1><p className="mt-1 text-sm text-[#64748B]">Enter the catalogue details using the existing supplier currency.</p></div>
    <form onSubmit={(event) => void submit(event)} className="space-y-5 rounded-xl border border-[#DCE5DF] bg-white p-5 shadow-sm sm:p-7" noValidate>
      {error && <div role="alert" className="flex items-center justify-between gap-3 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800"><span>{error}</span><button type="button" onClick={() => { setLoading(true); setReloadVersion((version) => version + 1); }} className="shrink-0 font-semibold underline">Retry</button></div>}{success && <p role="status" className="rounded-lg border border-[#B8D8C1] bg-[#E6F4EA] p-3 text-sm text-[#245C43]">{success}</p>}
      <div className="grid gap-5 sm:grid-cols-2">{fields.slice(0, 3).map((field) => <label key={field.key} className="grid gap-1.5 text-sm font-medium text-[#24352B]">{field.label}<input className={inputClass} maxLength={field.maxLength} value={values[field.key]} onChange={(event) => updateField(field.key, event.target.value)} aria-invalid={Boolean(fieldErrors[field.key])} aria-describedby={fieldErrors[field.key] ? `${field.key}-error` : undefined} />{fieldErrors[field.key] && <span id={`${field.key}-error`} className="text-xs font-normal text-red-700">{fieldErrors[field.key]}</span>}</label>)}
        <label className="grid gap-1.5 text-sm font-medium text-[#24352B]">Supplier<select className={inputClass} value={values.supplierId} onChange={(event) => updateField("supplierId", event.target.value)} aria-invalid={Boolean(fieldErrors.supplierId)}><option value="">Choose a supplier</option>{suppliers.map((supplier) => <option key={supplier.supplierId} value={supplier.supplierId}>{supplier.name} ({supplier.currencyCode})</option>)}</select>{fieldErrors.supplierId && <span className="text-xs font-normal text-red-700">{fieldErrors.supplierId}</span>}{suppliers.length === 0 && <span className="text-xs font-normal text-[#64748B]">No active suppliers available.</span>}</label>
        <label className="grid gap-1.5 text-sm font-medium text-[#24352B]">Unit price<input className={inputClass} type="number" min="0" step="0.0001" max="99999999999999.9999" value={values.unitPrice} onChange={(event) => updateField("unitPrice", event.target.value)} aria-invalid={Boolean(fieldErrors.unitPrice)} />{fieldErrors.unitPrice && <span className="text-xs font-normal text-red-700">{fieldErrors.unitPrice}</span>}</label>
        <label className="grid gap-1.5 text-sm font-medium text-[#24352B]">Pack size<input className={inputClass} maxLength={50} value={values.packSize} onChange={(event) => updateField("packSize", event.target.value)} aria-invalid={Boolean(fieldErrors.packSize)} />{fieldErrors.packSize && <span className="text-xs font-normal text-red-700">{fieldErrors.packSize}</span>}</label>
      </div>
      <div className="flex flex-col-reverse gap-3 border-t border-[#EDF1EE] pt-5 sm:flex-row sm:justify-end"><Link to={returnTo} className="rounded-lg border border-[#DCE5DF] px-4 py-2.5 text-center text-sm font-semibold text-[#52645A]">Cancel</Link><button disabled={saving || suppliers.length === 0} className="rounded-lg bg-[#438B68] px-5 py-2.5 text-sm font-semibold text-white hover:bg-[#367354] disabled:cursor-not-allowed disabled:opacity-50">{saving ? "Saving…" : editing ? "Save changes" : "Create product"}</button></div>
    </form></div>;
}

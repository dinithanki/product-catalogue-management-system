import { useCallback, useEffect, useState } from "react";
import { Link, useNavigate, useParams } from "react-router-dom";
import axios from "axios";
import { deleteProduct, getProduct, type Product } from "../api/productApi";
import { useAuthStore } from "../stores/authStore";

function money(amount: number, currency: string) {
  try { return new Intl.NumberFormat("en-LK", { style: "currency", currency, maximumFractionDigits: 2 }).format(amount); }
  catch { return `${currency} ${amount.toFixed(2)}`; }
}

function errorText(error: unknown) {
  if (axios.isAxiosError(error)) {
    const message = (error.response?.data as { message?: unknown } | undefined)?.message;
    if (typeof message === "string") return message;
    if (error.response?.status === 404) return "This product could not be found.";
    if (error.response?.status === 403) return "You do not have permission to view this product.";
    if (!error.response) return "Cannot connect to the server.";
  }
  return "Could not load the product. Please retry.";
}

export default function ProductDetailsPage() {
  const { productId } = useParams();
  const navigate = useNavigate();
  const isAdmin = useAuthStore((state) => state.user?.role.toLowerCase() === "admin");
  const [product, setProduct] = useState<Product | null>(null);
  const [error, setError] = useState("");
  const [loading, setLoading] = useState(true);
  const [actionError, setActionError] = useState("");
  const [deleting, setDeleting] = useState(false);
  const id = Number(productId);

  const load = useCallback(async () => {
    if (!Number.isInteger(id) || id < 1) { setError("Invalid product ID."); setLoading(false); return; }
    setLoading(true); setError("");
    try { setProduct(await getProduct(id)); }
    catch (requestError) { setError(errorText(requestError)); }
    finally { setLoading(false); }
  }, [id]);

  useEffect(() => { const timer = window.setTimeout(() => void load(), 0); return () => window.clearTimeout(timer); }, [load]);

  async function handleDelete() {
    if (!product || !window.confirm(`Soft-delete ${product.name}?`)) return;
    setDeleting(true); setActionError("");
    try { await deleteProduct(product.productId); navigate("/products", { replace: true }); }
    catch (requestError) { setActionError(errorText(requestError)); }
    finally { setDeleting(false); }
  }

  if (loading) return <p role="status" className="rounded-xl border border-[#DCE5DF] bg-white p-8 text-center text-[#64748B]">Loading product…</p>;
  if (error || !product) return <div><div role="alert" className="rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800">{error || "Product unavailable."}</div><button onClick={() => void load()} className="mt-3 font-semibold text-[#438B68]">Retry</button><Link to="/products" className="ml-4 text-sm text-[#438B68]">Back to products</Link></div>;

  return <div>
    <Link to="/products" className="text-sm font-semibold text-[#438B68] hover:underline">← Products</Link>
    <div className="mb-6 mt-3 flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between"><div><p className="text-sm font-semibold text-[#438B68]">PRODUCT DETAILS</p><h1 className="mt-1 text-2xl font-bold text-[#245C43]">{product.name}</h1><p className="mt-1 text-sm text-[#64748B]">SKU {product.sku}</p></div><div className="flex gap-2"><Link to={`/products/${product.productId}/edit`} className="rounded-lg border border-[#DCE5DF] bg-white px-4 py-2.5 text-sm font-semibold text-[#245C43]">Edit product</Link>{isAdmin && <button disabled={deleting} onClick={() => void handleDelete()} className="rounded-lg border border-red-200 bg-white px-4 py-2.5 text-sm font-semibold text-red-700 disabled:opacity-50">{deleting ? "Deleting…" : "Soft delete"}</button>}</div></div>
    {actionError && <p role="alert" className="mb-4 rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800">{actionError}</p>}
    <section className="rounded-xl border border-[#DCE5DF] bg-white p-5 shadow-sm sm:p-7"><h2 className="text-lg font-semibold text-[#245C43]">Product information</h2><dl className="mt-5 grid gap-x-8 gap-y-5 sm:grid-cols-2 lg:grid-cols-3">{[["Category", product.category], ["Supplier", product.supplierName], ["Pack size", product.packSize], ["Supplier price", money(product.unitPrice, product.currencyCode)], ["LKR equivalent", money(product.lkrEquivalent, "LKR")], ["Exchange rate", `${product.exchangeRateApplied.toLocaleString("en-LK", { maximumFractionDigits: 6 })} LKR per ${product.currencyCode}`], ["Exchange-rate timestamp (UTC)", new Date(product.exchangeRateTimestamp).toLocaleString("en-LK", { dateStyle: "medium", timeStyle: "short", timeZone: "UTC" })]].map(([label, value]) => <div key={label}><dt className="text-xs font-semibold uppercase tracking-wide text-[#64748B]">{label}</dt><dd className="mt-1 break-words text-sm text-[#24352B]">{value}</dd></div>)}</dl></section>
  </div>;
}

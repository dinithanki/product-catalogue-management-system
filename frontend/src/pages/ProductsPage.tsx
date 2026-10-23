import { useCallback, useEffect, useState } from "react";
import { Link } from "react-router-dom";
import axios from "axios";
import { getProducts, type Product, type ProductQueryParams, type ProductSortBy, type SortDirection } from "../api/productApi";
import { getActiveSuppliers, type ActiveSupplier } from "../api/supplierApi";
import { useAuthStore } from "../stores/authStore";

const columns: { key: ProductSortBy; label: string }[] = [
  { key: "name", label: "Product" },
  { key: "sku", label: "SKU" },
  { key: "category", label: "Category" },
  { key: "price", label: "Unit price" },
];

const controlClass = "rounded-lg border border-[#DCE5DF] bg-white px-3 py-2.5 text-sm text-[#24352B] outline-none focus:border-[#438B68] focus:ring-4 focus:ring-[#E6F4EA]";

function money(amount: number, currency: string) {
  try {
    return new Intl.NumberFormat("en-LK", { style: "currency", currency, maximumFractionDigits: 2 }).format(amount);
  } catch {
    return `${currency} ${amount.toFixed(2)}`;
  }
}

function apiError(error: unknown) {
  if (!axios.isAxiosError(error)) return "An unexpected error occurred.";
  const status = error.response?.status;
  const message = (error.response?.data as { message?: unknown } | undefined)?.message;
  if (typeof message === "string") return message;
  if (status === 403) return "You do not have permission to perform this action. If your account requires a password change, contact an administrator.";
  if (status === 404) return "The requested product was not found.";
  if (!error.response) return "Cannot connect to the server. Check your API URL and connection.";
  return `The request failed (HTTP ${status}). Please retry.`;
}

export default function ProductsPage() {
  const user = useAuthStore((state) => state.user);
  const [products, setProducts] = useState<Product[]>([]);
  const [suppliers, setSuppliers] = useState<ActiveSupplier[]>([]);
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [supplierId, setSupplierId] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [pageSize, setPageSize] = useState(10);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [sortBy, setSortBy] = useState<ProductSortBy>("name");
  const [sortDirection, setSortDirection] = useState<SortDirection>("ASC");
  const [sortTouched, setSortTouched] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");
  const [suppliersError, setSuppliersError] = useState("");

  const loadProducts = useCallback(async () => {
    setIsLoading(true);
    setError("");
    const params: ProductQueryParams = { pageNumber, pageSize, sortBy, sortDirection };
    if (search.trim()) params.search = search.trim();
    if (category.trim()) params.category = category.trim();
    if (supplierId) params.supplierId = Number(supplierId);
    try {
      const result = await getProducts(params);
      setProducts(result.items);
      setTotalCount(result.totalCount);
      setTotalPages(Math.max(1, result.totalPages));
    } catch (requestError) {
      setError(apiError(requestError));
    } finally {
      setIsLoading(false);
    }
  }, [pageNumber, pageSize, search, category, supplierId, sortBy, sortDirection]);

  useEffect(() => {
    const timer = window.setTimeout(() => void loadProducts(), 0);
    return () => window.clearTimeout(timer);
  }, [loadProducts]);

  useEffect(() => {
    getActiveSuppliers().then(setSuppliers).catch((requestError: unknown) => setSuppliersError(apiError(requestError)));
  }, []);

  function toggleSort(key: ProductSortBy) {
    setPageNumber(1);
    if (sortBy === key && sortTouched) setSortDirection((direction) => direction === "ASC" ? "DESC" : "ASC");
    else { setSortBy(key); setSortDirection("ASC"); setSortTouched(true); }
  }

  const isAdmin = user?.role.toLowerCase() === "admin";
  const sortButton = (key: ProductSortBy, label: string) => (
    <button type="button" onClick={() => toggleSort(key)} className="inline-flex items-center gap-1 text-left font-semibold text-[#52645A] hover:text-[#245C43]" aria-label={`Sort by ${label} ${sortBy === key ? sortDirection.toLowerCase() : "ascending"}`}>
      {label}<span aria-hidden="true" className="text-[#438B68]">{sortBy === key ? (sortDirection === "ASC" ? "↑" : "↓") : "↕"}</span>
    </button>
  );

  return <div>
    <div className="mb-6 flex flex-col gap-4 sm:flex-row sm:items-end sm:justify-between">
      <div><p className="text-sm font-semibold text-[#438B68]">CATALOGUE</p><h1 className="mt-1 text-2xl font-bold text-[#245C43]">Products</h1><p className="mt-1 text-sm text-[#64748B]">Search and manage supplier products and price conversions.</p></div>
      <Link to="/products/new" className="inline-flex justify-center rounded-lg bg-[#438B68] px-4 py-2.5 text-sm font-semibold text-white hover:bg-[#367354] focus:outline-none focus:ring-4 focus:ring-[#E6F4EA]">Add product</Link>
    </div>

    <section aria-label="Product filters" className="mb-5 grid gap-3 rounded-xl border border-[#DCE5DF] bg-white p-4 shadow-sm sm:grid-cols-2 lg:grid-cols-[minmax(220px,1fr)_minmax(160px,220px)_minmax(180px,240px)_auto]">
      <label className="grid gap-1.5 text-xs font-semibold text-[#52645A]">Search products<input className={controlClass} type="search" maxLength={100} value={search} onChange={(event) => { setSearch(event.target.value); setPageNumber(1); }} placeholder="Name or SKU" /></label>
      <label className="grid gap-1.5 text-xs font-semibold text-[#52645A]">Category<input className={controlClass} maxLength={100} value={category} onChange={(event) => { setCategory(event.target.value); setPageNumber(1); }} placeholder="Filter category" /></label>
      <label className="grid gap-1.5 text-xs font-semibold text-[#52645A]">Supplier<select className={controlClass} value={supplierId} onChange={(event) => { setSupplierId(event.target.value); setPageNumber(1); }}><option value="">All suppliers</option>{suppliers.map((supplier) => <option key={supplier.supplierId} value={supplier.supplierId}>{supplier.name}</option>)}</select></label>
      {suppliersError && <p role="status" className="self-end text-xs text-[#64748B] sm:col-span-2 lg:col-span-1">Supplier filter unavailable: {suppliersError}</p>}
    </section>

    {error && <div role="alert" className="mb-4 flex flex-col gap-3 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-800 sm:flex-row sm:items-center sm:justify-between"><p>{error}</p><button onClick={() => void loadProducts()} className="w-fit font-semibold underline">Retry</button></div>}
    <section className="overflow-hidden rounded-xl border border-[#DCE5DF] bg-white shadow-sm">
      <div className="flex items-center justify-between border-b border-[#DCE5DF] px-4 py-3"><p className="text-sm text-[#64748B]"><strong className="text-[#24352B]">{totalCount}</strong> products</p><label className="flex items-center gap-2 text-xs text-[#64748B]">Rows<select className="rounded-md border border-[#DCE5DF] bg-white px-2 py-1.5 text-[#24352B]" value={pageSize} onChange={(event) => { setPageSize(Number(event.target.value)); setPageNumber(1); }}><option value={10}>10</option><option value={25}>25</option><option value={50}>50</option><option value={100}>100</option></select></label></div>
      <div className="hidden overflow-x-auto md:block"><table className="w-full min-w-[950px] text-left text-sm"><thead className="bg-[#F2F7F3]"><tr>{columns.map((column) => <th key={column.key} scope="col" className="px-4 py-3">{sortButton(column.key, column.label)}</th>)}<th scope="col" className="px-4 py-3 text-xs font-semibold uppercase text-[#64748B]">Supplier</th><th scope="col" className="px-4 py-3 text-xs font-semibold uppercase text-[#64748B]">LKR equivalent</th><th scope="col" className="px-4 py-3 text-xs font-semibold uppercase text-[#64748B]">Pack size</th><th scope="col" className="px-4 py-3 text-xs font-semibold uppercase text-[#64748B]">Actions</th></tr></thead><tbody className="divide-y divide-[#EDF1EE]">{isLoading ? <tr><td colSpan={8} className="px-4 py-16 text-center text-[#64748B]">Loading products…</td></tr> : products.length === 0 ? <tr><td colSpan={8} className="px-4 py-16 text-center text-[#64748B]">No products match these filters.</td></tr> : products.map((product) => <tr key={product.productId} className="hover:bg-[#FAFCFA]"><td className="px-4 py-4"><Link to={`/products/${product.productId}`} className="font-semibold text-[#245C43] hover:underline">{product.name}</Link></td><td className="px-4 py-4 text-[#52645A]">{product.sku}</td><td className="px-4 py-4 text-[#52645A]">{product.category}</td><td className="px-4 py-4 whitespace-nowrap">{money(product.unitPrice, product.currencyCode)}</td><td className="px-4 py-4">{product.supplierName}</td><td className="px-4 py-4 whitespace-nowrap font-medium">{money(product.lkrEquivalent, "LKR")}</td><td className="px-4 py-4">{product.packSize}</td><td className="px-4 py-4"><div className="flex gap-3"><Link className="font-semibold text-[#438B68] hover:underline" to={`/products/${product.productId}/edit`}>Edit</Link>{isAdmin && <Link aria-label={`Manage ${product.name}`} className="font-semibold text-[#438B68] hover:underline" to={`/products/${product.productId}`}>Details</Link>}</div></td></tr>)}</tbody></table></div>
      <div className="divide-y divide-[#EDF1EE] md:hidden">{isLoading ? <p className="p-8 text-center text-sm text-[#64748B]">Loading products…</p> : products.length === 0 ? <p className="p-8 text-center text-sm text-[#64748B]">No products match these filters.</p> : products.map((product) => <article key={product.productId} className="space-y-3 p-4"><div className="flex items-start justify-between gap-3"><div><Link to={`/products/${product.productId}`} className="font-semibold text-[#245C43]">{product.name}</Link><p className="mt-1 text-xs text-[#64748B]">{product.sku} · {product.category}</p></div><Link to={`/products/${product.productId}/edit`} className="text-sm font-semibold text-[#438B68]">Edit</Link></div><dl className="grid grid-cols-2 gap-2 text-xs"><div><dt className="text-[#64748B]">Supplier</dt><dd>{product.supplierName}</dd></div><div><dt className="text-[#64748B]">Price</dt><dd>{money(product.unitPrice, product.currencyCode)}</dd></div><div><dt className="text-[#64748B]">LKR equivalent</dt><dd>{money(product.lkrEquivalent, "LKR")}</dd></div><div><dt className="text-[#64748B]">Pack size</dt><dd>{product.packSize}</dd></div></dl></article>)}</div>
    </section>
    <div className="mt-4 flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"><p className="text-sm text-[#64748B]">Page {pageNumber} of {totalPages}</p><div className="flex gap-2"><button className="rounded-lg border border-[#DCE5DF] bg-white px-4 py-2 text-sm font-medium disabled:opacity-40" disabled={pageNumber <= 1 || isLoading} onClick={() => setPageNumber((page) => page - 1)}>Previous</button><button className="rounded-lg border border-[#DCE5DF] bg-white px-4 py-2 text-sm font-medium disabled:opacity-40" disabled={pageNumber >= totalPages || isLoading} onClick={() => setPageNumber((page) => page + 1)}>Next</button></div></div>
    <div className="sr-only" aria-live="polite">{sortBy} sorted {sortDirection.toLowerCase()}</div>
  </div>;
}

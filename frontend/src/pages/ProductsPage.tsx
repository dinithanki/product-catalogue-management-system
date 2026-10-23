
import { useCallback, useEffect, useState } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import { getProducts } from "../api/productApi";
import type { Product, ProductQueryParams } from "../api/productApi";
import { useAuthStore } from "../stores/authStore";

export default function ProductsPage() {
  const navigate = useNavigate();
  const logout = useAuthStore((state) => state.logout);

  const [products, setProducts] = useState<Product[]>([]);
  const [search, setSearch] = useState("");
  const [category, setCategory] = useState("");
  const [pageNumber, setPageNumber] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [sortDescending, setSortDescending] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState("");

  function handleLogout() {
    logout();
    navigate("/login", { replace: true });
  }

  const loadProducts = useCallback(async () => {
    setIsLoading(true);
    setError("");

    try {
      const params: ProductQueryParams = {
        pageNumber,
        pageSize: 10,
        sortBy: "name",
        sortDescending,
      };

      if (search.trim()) {
        params.searchTerm = search.trim();
      }

      if (category) {
        params.category = category;
      }

      const result = await getProducts(params);

      setProducts(result.items);
      setTotalPages(Math.max(1, result.totalPages));
      setTotalCount(result.totalCount);
    } catch (err: unknown) {
      console.error("Failed to load products:", err);

      if (axios.isAxiosError(err)) {
        if (err.response?.status === 401) {
          setError("Your session has expired. Please sign in again.");
        } else {
          setError(
            `Could not load products (HTTP ${
              err.response?.status ?? "network error"
            }).`,
          );
        }
      } else {
        setError("An unexpected error occurred while loading products.");
      }
    } finally {
      setIsLoading(false);
    }
  }, [pageNumber, search, category, sortDescending]);

  useEffect(() => {
    void loadProducts();
  }, [loadProducts]);

  const categories = [
    ...new Set(products.map((product) => product.category)),
  ];

  function formatMoney(amount: number, currency: string) {
    return new Intl.NumberFormat("en-LK", {
      style: "currency",
      currency,
      minimumFractionDigits: 2,
      maximumFractionDigits: 2,
    }).format(amount);
  }

  function handleSearchChange(value: string) {
    setPageNumber(1);
    setSearch(value);
  }

  function handleCategoryChange(value: string) {
    setPageNumber(1);
    setCategory(value);
  }

  return (
    <main className="min-h-screen bg-slate-50 text-slate-900">
      {/* Header */}
      <header className="border-b border-slate-200 bg-white">
        <div className="mx-auto flex max-w-7xl flex-col gap-4 px-4 py-6 sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
          <div>
            <p className="text-sm font-bold uppercase tracking-[0.2em] text-blue-600">
              Keells
            </p>

            <h1 className="mt-1 text-2xl font-bold tracking-tight sm:text-3xl">
              Product Catalogue
            </h1>

            <p className="mt-1 text-sm text-slate-500">
              Browse products, supplier prices, and LKR equivalents.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-3">
            <div className="rounded-full bg-blue-50 px-4 py-2 text-sm font-semibold text-blue-700">
              {totalCount} {totalCount === 1 ? "product" : "products"}
            </div>

            <button
              type="button"
              onClick={handleLogout}
              className="rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-100 focus:outline-none focus:ring-4 focus:ring-blue-100"
            >
              Logout
            </button>
          </div>
        </div>
      </header>

      {/* Main content */}
      <section className="mx-auto max-w-7xl px-4 py-8 sm:px-6 lg:px-8">
        {/* Search and filters */}
        <div className="mb-6 rounded-xl border border-slate-200 bg-white p-4 shadow-sm sm:p-5">
          <div className="grid grid-cols-1 gap-4 md:grid-cols-[minmax(0,1fr)_220px_auto]">
            <div>
              <label
                htmlFor="product-search"
                className="mb-2 block text-sm font-medium text-slate-700"
              >
                Search products
              </label>

              <input
                id="product-search"
                type="search"
                value={search}
                onChange={(event) =>
                  handleSearchChange(event.target.value)
                }
                placeholder="Search by SKU or product name..."
                className="w-full rounded-lg border border-slate-300 bg-white px-4 py-3 text-sm outline-none transition focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
              />
            </div>

            <div>
              <label
                htmlFor="category-filter"
                className="mb-2 block text-sm font-medium text-slate-700"
              >
                Category
              </label>

              <select
                id="category-filter"
                value={category}
                onChange={(event) =>
                  handleCategoryChange(event.target.value)
                }
                className="w-full rounded-lg border border-slate-300 bg-white px-4 py-3 text-sm outline-none transition focus:border-blue-500 focus:ring-4 focus:ring-blue-100"
              >
                <option value="">All categories</option>

                {categories.map((item) => (
                  <option key={item} value={item}>
                    {item}
                  </option>
                ))}
              </select>
            </div>

            <div className="flex flex-col">
              <span className="mb-2 text-sm font-medium text-slate-700">
                Sort by
              </span>

              <button
                type="button"
                onClick={() =>
                  setSortDescending((current) => !current)
                }
                className="flex items-center justify-center gap-2 rounded-lg border border-slate-300 bg-white px-4 py-3 text-sm font-semibold transition hover:bg-slate-50 focus:outline-none focus:ring-4 focus:ring-blue-100"
              >
                Product name
                <span aria-hidden="true">
                  {sortDescending ? "↓" : "↑"}
                </span>
              </button>
            </div>
          </div>
        </div>

        {/* Error message */}
        {error && (
          <div
            role="alert"
            className="mb-6 flex flex-col gap-3 rounded-lg border border-red-200 bg-red-50 p-4 text-sm text-red-700 sm:flex-row sm:items-center sm:justify-between"
          >
            <p>{error}</p>

            <button
              type="button"
              onClick={() => void loadProducts()}
              className="w-fit font-semibold underline underline-offset-2"
            >
              Retry
            </button>
          </div>
        )}

        {/* Product table */}
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white shadow-sm">
          <div className="overflow-x-auto">
            <table className="w-full min-w-[1100px] table-fixed border-collapse text-left text-sm">
              <colgroup>
                <col className="w-[24%]" />
                <col className="w-[12%]" />
                <col className="w-[20%]" />
                <col className="w-[15%]" />
                <col className="w-[17%]" />
                <col className="w-[12%]" />
              </colgroup>

              <thead className="bg-slate-100">
                <tr className="border-b border-slate-200">
                  <th className="px-5 py-4 text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Product
                  </th>
                  <th className="px-5 py-4 text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Category
                  </th>
                  <th className="px-5 py-4 text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Supplier
                  </th>
                  <th className="px-5 py-4 text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Quoted price
                  </th>
                  <th className="px-5 py-4 text-xs font-semibold uppercase tracking-wider text-slate-600">
                    LKR equivalent
                  </th>
                  <th className="px-5 py-4 text-xs font-semibold uppercase tracking-wider text-slate-600">
                    Pack size
                  </th>
                </tr>
              </thead>

              <tbody className="divide-y divide-slate-100">
                {isLoading ? (
                  <tr>
                    <td
                      colSpan={6}
                      className="px-5 py-16 text-center text-slate-500"
                    >
                      <div className="flex flex-col items-center gap-3">
                        <span
                          className="h-6 w-6 animate-spin rounded-full border-2 border-slate-200 border-t-blue-600"
                          aria-hidden="true"
                        />
                        <span>Loading products...</span>
                      </div>
                    </td>
                  </tr>
                ) : products.length === 0 ? (
                  <tr>
                    <td
                      colSpan={6}
                      className="px-5 py-16 text-center"
                    >
                      <p className="font-semibold text-slate-700">
                        No products found
                      </p>
                      <p className="mt-1 text-sm text-slate-500">
                        Try changing your search or category filter.
                      </p>
                    </td>
                  </tr>
                ) : (
                  products.map((product) => (
                    <tr
                      key={product.productId}
                      className="transition-colors hover:bg-slate-50"
                    >
                      <td className="break-words px-5 py-5 align-middle">
                        <p className="font-semibold leading-5 text-slate-900">
                          {product.name}
                        </p>
                        <p className="mt-1.5 text-xs text-slate-500">
                          SKU: {product.sku}
                        </p>
                      </td>

                      <td className="break-words px-5 py-5 align-middle text-slate-600">
                        {product.category}
                      </td>

                      <td className="break-words px-5 py-5 align-middle text-slate-600">
                        {product.supplierName}
                      </td>

                      <td className="whitespace-nowrap px-5 py-5 align-middle font-medium text-slate-800">
                        {formatMoney(
                          product.unitPrice,
                          product.currencyCode,
                        )}
                      </td>

                      <td className="whitespace-nowrap px-5 py-5 align-middle font-semibold text-slate-900">
                        {formatMoney(product.lkrEquivalent, "LKR")}
                      </td>

                      <td className="break-words px-5 py-5 align-middle text-slate-600">
                        {product.packSize}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>

        {/* Pagination */}
        <div className="mt-5 flex flex-col gap-4 rounded-xl border border-slate-200 bg-white px-4 py-4 shadow-sm sm:flex-row sm:items-center sm:justify-between">
          <div className="text-sm text-slate-600">
            Page{" "}
            <span className="font-semibold text-slate-900">
              {pageNumber}
            </span>{" "}
            of{" "}
            <span className="font-semibold text-slate-900">
              {totalPages}
            </span>
            <span className="mx-2 text-slate-300">|</span>
            <span>{totalCount} total products</span>
          </div>

          <div className="flex items-center gap-3">
            <button
              type="button"
              disabled={pageNumber <= 1 || isLoading}
              onClick={() =>
                setPageNumber((page) => Math.max(1, page - 1))
              }
              className="min-w-[100px] rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 focus:outline-none focus:ring-4 focus:ring-blue-100 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Previous
            </button>

            <button
              type="button"
              disabled={pageNumber >= totalPages || isLoading}
              onClick={() =>
                setPageNumber((page) =>
                  Math.min(totalPages, page + 1),
                )
              }
              className="min-w-[100px] rounded-lg border border-slate-300 bg-white px-4 py-2.5 text-sm font-semibold text-slate-700 transition hover:bg-slate-50 focus:outline-none focus:ring-4 focus:ring-blue-100 disabled:cursor-not-allowed disabled:opacity-40"
            >
              Next
            </button>
          </div>
        </div>
      </section>
    </main>
  );
}

import axiosInstance from "./axiosInstance";

export interface Product {
  productId: number;
  sku: string;
  name: string;
  category: string;
  supplierId: number;
  supplierName: string;
  currencyCode: string;
  unitPrice: number;
  packSize: string;
  lkrEquivalent: number;
  exchangeRateApplied: number;
  exchangeRateTimestamp: string;
}

export interface ProductListResponse {
  items: Product[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export type ProductSortBy = "name" | "sku" | "category" | "price";
export type SortDirection = "ASC" | "DESC";

export interface ProductQueryParams {
  pageNumber?: number;
  pageSize?: number;
  search?: string;
  category?: string;
  supplierId?: number;
  sortBy?: ProductSortBy;
  sortDirection?: SortDirection;
}

export interface ProductRequest {
  sku: string;
  name: string;
  category: string;
  supplierId: number;
  unitPrice: number;
  packSize: string;
}

export async function getProducts(params: ProductQueryParams = {}) {
  const { data } = await axiosInstance.get<ProductListResponse>("/products", { params });
  return data;
}

export async function getProduct(id: number) {
  const { data } = await axiosInstance.get<Product>(`/products/${id}`);
  return data;
}

export async function createProduct(product: ProductRequest) {
  const { data } = await axiosInstance.post<{ productId: number; message: string }>("/products", product);
  return data;
}

export async function updateProduct(id: number, product: ProductRequest) {
  const { data } = await axiosInstance.put<{ message: string }>(`/products/${id}`, product);
  return data;
}

export async function deleteProduct(id: number) {
  await axiosInstance.delete(`/products/${id}`);
}

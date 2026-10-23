
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

export interface ProductQueryParams {
  pageNumber?: number;
  pageSize?: number;
  searchTerm?: string;
  category?: string;
  supplierId?: number;
  sortBy?: string;
  sortDescending?: boolean;
}

export async function getProducts(
  params: ProductQueryParams = {},
): Promise<ProductListResponse> {
  const response = await axiosInstance.get<ProductListResponse>(
    "/products",
    { params },
  );

  return response.data;
}

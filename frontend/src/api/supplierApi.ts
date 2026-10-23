import axiosInstance from "./axiosInstance";

export interface Supplier {
  supplierId: number;
  name: string;
  country: string;
  currencyCode: string;
  contactNumber: string;
  email: string;
  isActive: boolean;
}

export interface ActiveSupplier {
  supplierId: number;
  name: string;
  country: string;
  currencyCode: string;
}

export interface SupplierRequest {
  name: string;
  country: string;
  currencyCode: string;
  contactNumber: string;
  email: string;
}

export async function getSuppliers() {
  const { data } = await axiosInstance.get<Supplier[]>("/suppliers");
  return data;
}

export async function getActiveSuppliers() {
  const { data } = await axiosInstance.get<ActiveSupplier[]>("/suppliers/active");
  return data;
}

export async function createSupplier(supplier: SupplierRequest) {
  const { data } = await axiosInstance.post<{ supplierId: number; message: string }>("/suppliers", supplier);
  return data;
}

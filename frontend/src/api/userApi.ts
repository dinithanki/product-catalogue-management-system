import axiosInstance from "./axiosInstance";

export interface ManagedUser {
  userId: number;
  username: string;
  role: "Admin" | "User";
  mustChangePassword: boolean;
  isActive: boolean;
}

export interface UserRequest {
  username: string;
  role: "Admin" | "User";
}

export interface TemporaryPasswordResponse {
  message: string;
  temporaryPassword: string;
  mustChangePassword: boolean;
}

export async function getUsers() {
  const { data } = await axiosInstance.get<ManagedUser[]>("/users");
  return data;
}

export async function createUser(request: UserRequest) {
  const { data } = await axiosInstance.post<TemporaryPasswordResponse & { userId: number }>("/users", request);
  return data;
}

export async function updateUser(userId: number, request: UserRequest) {
  const { data } = await axiosInstance.put<{ message: string }>(`/users/${userId}`, request);
  return data;
}

export async function resetUserPassword(userId: number) {
  const { data } = await axiosInstance.post<TemporaryPasswordResponse>(`/users/${userId}/reset-password`);
  return data;
}

export async function activateUser(userId: number) {
  const { data } = await axiosInstance.patch<{ message: string }>(`/users/${userId}/activate`);
  return data;
}

export async function deactivateUser(userId: number) {
  const { data } = await axiosInstance.patch<{ message: string }>(`/users/${userId}/deactivate`);
  return data;
}

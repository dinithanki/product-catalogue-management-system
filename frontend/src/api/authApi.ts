import axiosInstance from "./axiosInstance";
import type {
  LoginRequest,
  LoginResponse,
  LogoutResponse,
  ChangePasswordRequest,
} from "../types/auth";

export type { LoginRequest, LoginResponse } from "../types/auth";

export async function login(credentials: LoginRequest): Promise<LoginResponse> {
  const response = await axiosInstance.post<LoginResponse>(
    "/auth/login",
    credentials,
  );
  return response.data;
}

export async function refreshSession(): Promise<LoginResponse> {
  const response = await axiosInstance.post<LoginResponse>(
    "/auth/refresh",
  );
  return response.data;
}

export async function logoutSession(): Promise<LogoutResponse> {
  const response = await axiosInstance.post<LogoutResponse>(
    "/auth/logout",
  );
  return response.data;
}

export async function changePassword(request: ChangePasswordRequest): Promise<{ message: string }> {
  const response = await axiosInstance.post<{ message: string }>(
    "/auth/change-password",
    request,
  );
  return response.data;
}

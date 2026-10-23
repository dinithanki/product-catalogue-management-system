
import axiosInstance from "./axiosInstance";

export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiresAtUtc: string;
  userId: number;
  username: string;
  role: string;
}

export async function login(
  credentials: LoginRequest,
): Promise<LoginResponse> {
  const response = await axiosInstance.post<LoginResponse>(
    "/auth/login",
    credentials,
  );

  return response.data;
}

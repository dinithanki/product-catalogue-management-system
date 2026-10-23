export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthUser {
  userId: number;
  username: string;
  role: string;
}

export interface AuthTokenResponse {
  accessToken: string;
  expiresAtUtc: string;
  userId: number;
  username: string;
  role: string;
  mustChangePassword: boolean;
}

export type LoginResponse = AuthTokenResponse;

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}

export interface LogoutResponse {
  message: string;
}

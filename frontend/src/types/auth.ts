
export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthUser {
  userId: number;
  username: string;
  role: string;
}

export interface LoginResponse {
  message: string;
  token: string;
}

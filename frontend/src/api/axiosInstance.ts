import axios, { type InternalAxiosRequestConfig } from "axios";
import { refreshSession } from "./authApi";
import { useAuthStore } from "../stores/authStore";

declare module "axios" {
  interface InternalAxiosRequestConfig {
    _authRetry?: boolean;
    _accessTokenAtRequest?: string | null;
  }
}

const axiosInstance = axios.create({
  baseURL: import.meta.env.VITE_API_BASE_URL,
  headers: { "Content-Type": "application/json" },
  timeout: 10000,
  withCredentials: true,
});

let refreshInFlight: Promise<void> | null = null;

axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  if (isAuthEndpoint(config.url)) {
    delete config.headers.Authorization;
    return config;
  }

  const token = useAuthStore.getState().accessToken;
  config._accessTokenAtRequest = token;

  if (token) config.headers.Authorization = `Bearer ${token}`;
  else delete config.headers.Authorization;

  return config;
});

axiosInstance.interceptors.response.use(
  (response) => response,
  async (error: unknown) => {
    if (!axios.isAxiosError(error) || error.response?.status !== 401) {
      return Promise.reject(error);
    }

    const config = error.config;
    if (!config || config._authRetry || isAuthEndpoint(config.url)) {
      return Promise.reject(error);
    }
    config._authRetry = true;

    const auth = useAuthStore.getState();
    try {
      // If another request already rotated the token, reuse that session.
      if (auth.accessToken === config._accessTokenAtRequest) {
        refreshInFlight ??= refreshSession()
          .then((session) => {
            useAuthStore.getState().setSession(session);
          })
          .finally(() => {
            refreshInFlight = null;
          });
        await refreshInFlight;
      }

      return axiosInstance(config);
    } catch {
      useAuthStore.getState().clearSession();
      return Promise.reject(error);
    }
  },
);

function isAuthEndpoint(url?: string): boolean {
  return Boolean(url && /^\/auth\/(login|refresh|logout)(?:$|\?)/.test(url));
}

export default axiosInstance;

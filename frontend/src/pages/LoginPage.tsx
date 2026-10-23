
import { useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import axios from "axios";
import { login } from "../api/authApi";
import { useAuthStore } from "../stores/authStore";

export default function LoginPage() {
  const navigate = useNavigate();
  const setAccessToken = useAuthStore((state) => state.setAccessToken);

  const [username, setUsername] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [isLoading, setIsLoading] = useState(false);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");

    if (!username.trim() || !password) {
      setError("Please enter your username and password.");
      return;
    }

    setIsLoading(true);

    try {
      const response = await login({
        username: username.trim(),
        password,
      });

      if (!response.accessToken) {
        setError("Login response is missing the access token.");
        return;
      }

      setAccessToken(response.accessToken);
      navigate("/products", { replace: true });
    } catch (err: unknown) {
      console.error("Login error:", err);

      if (axios.isAxiosError(err)) {
        const status = err.response?.status;
        const data = err.response?.data;

        const serverMessage =
          typeof data?.message === "string" ? data.message : null;

        if (serverMessage) {
          setError(serverMessage);
        } else if (!err.response) {
          setError(
            "Cannot connect to the server. Check the API URL and backend connection.",
          );
        } else {
          setError(`Login failed (HTTP ${status}). Please try again.`);
        }
      } else {
        setError("An unexpected error occurred. Please try again.");
      }
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <main className="flex min-h-screen items-center justify-center bg-slate-950 px-4 py-10">
      <section className="w-full max-w-md rounded-2xl bg-white p-8 shadow-2xl sm:p-10">
        <div className="mb-7 flex h-12 w-12 items-center justify-center rounded-xl bg-blue-600 text-xl font-bold text-white">
          K
        </div>

        <p className="text-sm font-semibold uppercase tracking-widest text-blue-600">
          Keells Product Catalogue
        </p>

        <h1 className="mt-3 text-3xl font-bold tracking-tight text-slate-900">
          Welcome back
        </h1>

        <p className="mt-2 text-sm text-slate-500">
          Sign in to manage your product catalogue.
        </p>

        <form onSubmit={handleSubmit} className="mt-8 space-y-5">
          <div>
            <label
              htmlFor="username"
              className="mb-2 block text-sm font-medium text-slate-700"
            >
              Username
            </label>

            <input
              id="username"
              name="username"
              autoComplete="username"
              value={username}
              onChange={(event) => setUsername(event.target.value)}
              className="w-full rounded-lg border border-slate-300 px-4 py-3 text-slate-900 outline-none transition focus:border-blue-600 focus:ring-2 focus:ring-blue-100"
              placeholder="Enter your username"
              required
              disabled={isLoading}
            />
          </div>

          <div>
            <label
              htmlFor="password"
              className="mb-2 block text-sm font-medium text-slate-700"
            >
              Password
            </label>

            <input
              id="password"
              name="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              className="w-full rounded-lg border border-slate-300 px-4 py-3 text-slate-900 outline-none transition focus:border-blue-600 focus:ring-2 focus:ring-blue-100"
              placeholder="Enter your password"
              required
              disabled={isLoading}
            />
          </div>

          {error && (
            <p
              role="alert"
              className="rounded-lg bg-red-50 px-4 py-3 text-sm text-red-700"
            >
              {error}
            </p>
          )}

          <button
            type="submit"
            disabled={isLoading}
            className="w-full rounded-lg bg-blue-600 px-4 py-3 font-semibold text-white transition hover:bg-blue-700 disabled:cursor-not-allowed disabled:opacity-60"
          >
            {isLoading ? "Signing in..." : "Sign in"}
          </button>
        </form>
      </section>
    </main>
  );
}

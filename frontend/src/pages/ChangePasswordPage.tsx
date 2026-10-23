import { useState, type FormEvent } from "react";
import axios from "axios";
import { useNavigate } from "react-router-dom";
import { changePassword } from "../api/authApi";
import { useAuthStore } from "../stores/authStore";

export default function ChangePasswordPage() {
  const navigate = useNavigate();
  const markPasswordChanged = useAuthStore((state) => state.markPasswordChanged);
  const [currentPassword, setCurrentPassword] = useState("");
  const [newPassword, setNewPassword] = useState("");
  const [confirmPassword, setConfirmPassword] = useState("");
  const [error, setError] = useState("");
  const [isSaving, setIsSaving] = useState(false);
  const [isComplete, setIsComplete] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setError("");
    if (!currentPassword || !newPassword) {
      setError("Enter your current password and a new password.");
      return;
    }
    if (newPassword.length < 12 || newPassword.length > 128) {
      setError("Your new password must be between 12 and 128 characters.");
      return;
    }
    if (newPassword === currentPassword) {
      setError("Your new password must be different from your current password.");
      return;
    }
    if (newPassword !== confirmPassword) {
      setError("The new password and confirmation do not match.");
      return;
    }

    setIsSaving(true);
    try {
      await changePassword({ currentPassword, newPassword });
      setCurrentPassword("");
      setNewPassword("");
      setConfirmPassword("");
      setIsComplete(true);
    } catch (requestError) {
      if (axios.isAxiosError(requestError)) {
        const message = (requestError.response?.data as { message?: unknown } | undefined)?.message;
        setError(typeof message === "string" ? message : !requestError.response ? "Cannot connect to the server. Please retry." : `Password change failed (HTTP ${requestError.response.status}).`);
      } else setError("Password change failed. Please retry.");
    } finally {
      setIsSaving(false);
    }
  }

  return <main className="flex min-h-screen items-center justify-center bg-[#F8FAF9] px-4 py-10">
    <section className="w-full max-w-md rounded-2xl border border-[#DCE5DF] bg-white p-7 shadow-sm sm:p-10">
      <div className="mb-6 grid h-12 w-12 place-items-center rounded-xl bg-[#438B68] text-xl font-bold text-white">K</div>
      <p className="text-sm font-semibold uppercase tracking-widest text-[#438B68]">Keells Product Catalogue</p>
      <h1 className="mt-3 text-2xl font-bold text-[#245C43]">Change your password</h1>
      <p className="mt-2 text-sm text-[#64748B]">Your account must use a new password before you can continue.</p>
      {isComplete ? <div className="mt-6 space-y-4"><p role="status" className="rounded-lg border border-[#B8D8C1] bg-[#E6F4EA] p-3 text-sm text-[#245C43]">Password changed successfully.</p><button onClick={() => { markPasswordChanged(); navigate("/products", { replace: true }); }} className="w-full rounded-lg bg-[#438B68] px-4 py-3 font-semibold text-white hover:bg-[#367354]">Continue to catalogue</button></div> : <form onSubmit={(event) => void submit(event)} className="mt-7 space-y-4">
        <label className="grid gap-1.5 text-sm font-medium text-[#24352B]">Current password<input type="password" autoComplete="current-password" value={currentPassword} onChange={(event) => setCurrentPassword(event.target.value)} className="w-full rounded-lg border border-[#DCE5DF] px-3 py-2.5 outline-none focus:border-[#438B68] focus:ring-4 focus:ring-[#E6F4EA]" required disabled={isSaving} /></label>
        <label className="grid gap-1.5 text-sm font-medium text-[#24352B]">New password<input type="password" autoComplete="new-password" minLength={12} maxLength={128} value={newPassword} onChange={(event) => setNewPassword(event.target.value)} className="w-full rounded-lg border border-[#DCE5DF] px-3 py-2.5 outline-none focus:border-[#438B68] focus:ring-4 focus:ring-[#E6F4EA]" required disabled={isSaving} /><span className="text-xs font-normal text-[#64748B]">Use 12 to 128 characters.</span></label>
        <label className="grid gap-1.5 text-sm font-medium text-[#24352B]">Confirm new password<input type="password" autoComplete="new-password" maxLength={128} value={confirmPassword} onChange={(event) => setConfirmPassword(event.target.value)} className="w-full rounded-lg border border-[#DCE5DF] px-3 py-2.5 outline-none focus:border-[#438B68] focus:ring-4 focus:ring-[#E6F4EA]" required disabled={isSaving} /></label>
        {error && <p role="alert" className="rounded-lg border border-red-200 bg-red-50 p-3 text-sm text-red-800">{error}</p>}
        <button type="submit" disabled={isSaving} className="w-full rounded-lg bg-[#438B68] px-4 py-3 font-semibold text-white hover:bg-[#367354] focus:outline-none focus:ring-4 focus:ring-[#E6F4EA] disabled:opacity-50">{isSaving ? "Updating…" : "Update password"}</button>
      </form>}
    </section>
  </main>;
}

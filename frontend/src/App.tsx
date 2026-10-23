
function App() {
  return (
    <main className="min-h-screen bg-slate-950 flex items-center justify-center p-6">
      <div className="w-full max-w-md rounded-2xl bg-white p-8 shadow-2xl">
        <div className="mb-6 flex h-14 w-14 items-center justify-center rounded-xl bg-blue-600 text-2xl font-bold text-white">
          K
        </div>

        <h1 className="text-3xl font-bold tracking-tight text-slate-900">
          Keells Product Catalogue
        </h1>

        <p className="mt-3 text-slate-600">
          React, TypeScript and Tailwind CSS are ready to build your application.
        </p>

        <button
          onClick={() => alert("Tailwind CSS is working!")}
          className="mt-6 w-full rounded-lg bg-blue-600 px-4 py-3 font-semibold text-white transition hover:bg-blue-700"
        >
          Test Tailwind CSS
        </button>

        <p className="mt-4 text-center text-sm text-green-700">
          Frontend setup test
        </p>
      </div>
    </main>
  );
}

export default App;

export default function Home() {
  return (
    <div className="min-h-screen flex flex-col items-center justify-center gap-6 p-8">
      <div className="text-center space-y-2">
        <h1 className="text-3xl font-semibold">WE Platform</h1>
        <p className="text-black/60">
          Educational Intelligence Platform — teacher portal shell
        </p>
      </div>
      <a
        href="/login"
        className="rounded bg-black text-white px-6 py-2 font-medium"
      >
        Sign in
      </a>
    </div>
  );
}

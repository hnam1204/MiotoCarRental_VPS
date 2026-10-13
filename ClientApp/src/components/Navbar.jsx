import React from 'react';

export default function Navbar({ stats }) {
  return (
    <header className="bg-white/90 backdrop-blur-md border-b border-slate-200 sticky top-0 z-40">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
        <div className="flex items-center gap-3">
          <a href="/" className="flex items-center gap-2.5 font-black text-lg text-slate-900">
            <span className="w-9 h-9 rounded-xl bg-emerald-600 text-white flex items-center justify-center shadow-sm">
              <i className="bi bi-lightning-charge-fill"></i>
            </span>
            <span>EcoBike <span className="text-emerald-600">ReactJS</span></span>
          </a>
          <span className="hidden sm:inline-block px-2.5 py-1 rounded-lg bg-sky-50 text-sky-700 border border-sky-200 text-xs font-bold">
            ClientApp (React 18) + .NET 9 Web API
          </span>
        </div>

        <div className="flex items-center gap-3 text-xs font-bold">
          {stats && (
            <div className="hidden md:flex items-center gap-3 px-3 py-1.5 rounded-xl bg-slate-100 text-slate-700">
              <span>⚡ {stats.totalVehicles} Xe điện</span>
              <span>•</span>
              <span>👤 {stats.totalUsers} Users</span>
              <span>•</span>
              <span>📋 {stats.totalBookings} Đơn</span>
            </div>
          )}
          <a
            href="/swagger"
            className="px-3.5 py-2 rounded-xl bg-amber-50 hover:bg-amber-100 text-amber-800 border border-amber-200 transition"
          >
            <i className="bi bi-braces mr-1"></i> .NET API Swagger
          </a>
          <a
            href="/"
            className="px-3.5 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white transition"
          >
            <i className="bi bi-grid-fill mr-1"></i> Cổng Chính EcoBike
          </a>
        </div>
      </div>
    </header>
  );
}

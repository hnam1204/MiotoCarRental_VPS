import React from 'react';

export default function Navbar() {
  return (
    <header className="bg-white border-b border-gray-200 sticky top-0 z-40">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 h-16 flex items-center justify-between">
        <a href="/" className="flex items-center gap-2 font-bold text-lg text-gray-900">
          <span className="w-8 h-8 rounded-lg bg-emerald-600 text-white flex items-center justify-center">
            <i className="bi bi-bicycle"></i>
          </span>
          <span>Eco<span className="text-emerald-600">Bike</span></span>
        </a>

        <div className="flex items-center gap-5 text-sm font-medium text-gray-600">
          <a href="/" className="hover:text-emerald-600 transition">Trang chủ</a>
          <a href="/Vehicles" className="hover:text-emerald-600 transition">Thuê xe điện</a>
          <a href="/Stations" className="hover:text-emerald-600 transition">Trạm sạc</a>
        </div>
      </div>
    </header>
  );
}

import React, { useEffect, useState } from 'react';
import {
  fetchVehicles,
  fetchSystemStats,
  fetchCoupons,
  fetchStations,
  calculateBookingQuote
} from './api/ecobikeApi';
import Navbar from './components/Navbar';
import VehicleCard from './components/VehicleCard';
import BookingModal from './components/BookingModal';

const CATEGORIES = [
  { id: '', label: 'Tất cả dòng xe', badge: '12 xe' },
  { id: '0', label: 'Học sinh / SV (<50km/h)', badge: 'Miễn bằng A1' },
  { id: '1', label: 'Tầm trung / Đi phố', badge: '100-120k/ngày' },
  { id: '2', label: 'Đi phượt / Pin trâu', badge: '200-270km/sạc' },
  { id: '3', label: 'Thể thao / Cao cấp', badge: 'ABS 2 kênh' }
];

export default function App() {
  const [vehicles, setVehicles] = useState([]);
  const [stats, setStats] = useState(null);
  const [coupons, setCoupons] = useState([]);
  const [stations, setStations] = useState([]);
  const [loading, setLoading] = useState(true);

  const [category, setCategory] = useState('');
  const [city, setCity] = useState('');
  const [search, setSearch] = useState('');
  const [selectedVehicle, setSelectedVehicle] = useState(null);

  useEffect(() => {
    Promise.all([fetchSystemStats(), fetchCoupons(), fetchStations()])
      .then(([statsData, couponsData, stationsData]) => {
        setStats(statsData);
        setCoupons(couponsData);
        setStations(stationsData);
      })
      .catch(console.error);
  }, []);

  useEffect(() => {
    setLoading(true);
    fetchVehicles({ category, city, search })
      .then((data) => setVehicles(data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [category, city, search]);

  return (
    <div className="min-h-screen flex flex-col bg-slate-50">
      <Navbar stats={stats} />

      <main className="max-w-7xl mx-auto w-full px-4 sm:px-6 py-8 space-y-8">
        {/* Filter Bar */}
        <section className="bg-white rounded-3xl p-6 shadow-sm border border-slate-100 space-y-5">
          <div className="flex flex-wrap items-center justify-between gap-4">
            <div>
              <span className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full text-xs font-bold bg-emerald-50 text-emerald-700 border border-emerald-200">
                React 18 SPA + ASP.NET Core 9.0 Web API
              </span>
              <h1 className="text-2xl font-extrabold text-slate-900 mt-2">
                Khám phá Đội Xe Máy Điện Thông Minh EcoBike
              </h1>
            </div>

            <div className="flex flex-wrap gap-3">
              <input
                type="text"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder="Tìm hãng xe, dòng xe (VinFast, Dat Bike...)"
                className="px-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:border-emerald-500 w-64"
              />
              <select
                value={city}
                onChange={(e) => setCity(e.target.value)}
                className="px-4 py-2.5 rounded-xl border border-slate-200 text-sm focus:outline-none focus:border-emerald-500"
              >
                <option value="">Tất cả thành phố</option>
                <option value="TP. Hồ Chí Minh">TP. Hồ Chí Minh</option>
                <option value="Hà Nội">Hà Nội</option>
                <option value="Đà Nẵng">Đà Nẵng</option>
              </select>
            </div>
          </div>

          {/* 4 Category Tabs */}
          <div className="flex flex-wrap gap-2 pt-2 border-t border-slate-100">
            {CATEGORIES.map((cat) => (
              <button
                key={cat.id}
                onClick={() => setCategory(cat.id)}
                className={`px-4 py-2 rounded-xl text-xs font-bold transition flex items-center gap-2 ${
                  category === cat.id
                    ? 'bg-emerald-600 text-white shadow-sm'
                    : 'bg-slate-100 text-slate-700 hover:bg-slate-200'
                }`}
              >
                <span>{cat.label}</span>
                <span className="px-2 py-0.5 rounded-full text-[10px] bg-black/10">
                  {cat.badge}
                </span>
              </button>
            ))}
          </div>
        </section>

        {/* Vehicle Grid */}
        {loading ? (
          <div className="text-center py-16 text-slate-500 font-semibold">
            Đang tải dữ liệu từ ASP.NET Core 9.0 Web API (/api/vehicles)...
          </div>
        ) : (
          <section className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
            {vehicles.map((v) => (
              <VehicleCard
                key={v.id}
                vehicle={v}
                onSelect={() => setSelectedVehicle(v)}
              />
            ))}
          </section>
        )}
      </main>

      {selectedVehicle && (
        <BookingModal
          vehicle={selectedVehicle}
          coupons={coupons}
          onCalculate={calculateBookingQuote}
          onClose={() => setSelectedVehicle(null)}
        />
      )}
    </div>
  );
}

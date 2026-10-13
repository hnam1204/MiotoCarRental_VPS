import React from 'react';

export default function VehicleCard({ vehicle, onSelect }) {
  const formatVnd = (n) => Number(n || 0).toLocaleString('vi-VN') + 'đ';

  return (
    <div className="bg-white rounded-3xl border border-slate-100 shadow-sm hover:shadow-md transition overflow-hidden flex flex-col">
      <div className="relative h-48 bg-slate-100 overflow-hidden">
        <img
          src={vehicle.thumbnailUrl}
          alt={`${vehicle.brand} ${vehicle.model}`}
          className="w-full h-full object-cover"
        />
        <div className="absolute top-3 left-3 flex flex-wrap gap-1.5">
          <span className="px-2.5 py-1 rounded-full text-[11px] font-bold bg-slate-900/80 text-white backdrop-blur-sm">
            {vehicle.category}
          </span>
          {!vehicle.requiresLicenseA1 && (
            <span className="px-2.5 py-1 rounded-full text-[11px] font-bold bg-emerald-600 text-white">
              Miễn bằng A1 (Học sinh/SV)
            </span>
          )}
        </div>
      </div>

      <div className="p-5 flex-1 flex flex-col justify-between space-y-4">
        <div>
          <div className="text-xs font-bold text-emerald-600 uppercase tracking-wider">
            {vehicle.brand} • {vehicle.city}
          </div>
          <h3 className="text-lg font-extrabold text-slate-900 mt-0.5">
            {vehicle.brand} {vehicle.model}
          </h3>

          <div className="grid grid-cols-3 gap-2 mt-3 text-center bg-slate-50 rounded-2xl p-2.5 text-xs">
            <div>
              <div className="text-slate-400">Quãng đường</div>
              <div className="font-bold text-slate-800">{vehicle.batteryRangeKm} km</div>
            </div>
            <div>
              <div className="text-slate-400">Vận tốc</div>
              <div className="font-bold text-slate-800">{vehicle.maxSpeedKmH} km/h</div>
            </div>
            <div>
              <div className="text-slate-400">Bằng lái</div>
              <div className="font-bold text-emerald-700">
                {vehicle.requiresLicenseA1 ? 'Cần A1' : 'Miễn A1'}
              </div>
            </div>
          </div>
        </div>

        <div className="pt-3 border-t border-slate-100 flex items-center justify-between">
          <div>
            <span className="text-xl font-black text-emerald-600">
              {formatVnd(vehicle.pricePerDay)}
            </span>
            <span className="text-xs text-slate-400"> / ngày</span>
          </div>
          <div className="flex gap-2">
            <button
              onClick={onSelect}
              className="px-3.5 py-2 rounded-xl bg-emerald-50 hover:bg-emerald-100 text-emerald-700 font-bold text-xs transition"
            >
              Tính phí API
            </button>
            <a
              href={`/Vehicles/Details/${vehicle.id}`}
              className="px-3.5 py-2 rounded-xl bg-emerald-600 hover:bg-emerald-700 text-white font-bold text-xs transition"
            >
              Đặt xe ngay
            </a>
          </div>
        </div>
      </div>
    </div>
  );
}

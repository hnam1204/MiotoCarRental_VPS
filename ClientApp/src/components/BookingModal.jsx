import React, { useEffect, useState } from 'react';

export default function BookingModal({ vehicle, coupons, onCalculate, onClose }) {
  const [days, setDays] = useState(2);
  const [couponCode, setCouponCode] = useState('');
  const [quote, setQuote] = useState(null);

  useEffect(() => {
    onCalculate(vehicle.id, days, couponCode)
      .then(setQuote)
      .catch(console.error);
  }, [vehicle.id, days, couponCode]);

  const formatVnd = (n) => Number(n || 0).toLocaleString('vi-VN') + 'đ';

  return (
    <div className="fixed inset-0 z-50 bg-slate-900/60 backdrop-blur-sm flex items-center justify-center p-4">
      <div className="bg-white rounded-3xl max-w-md w-full p-6 shadow-2xl space-y-5">
        <div className="flex items-center justify-between border-b border-slate-100 pb-3">
          <div>
            <span className="text-[11px] font-bold uppercase text-emerald-600">
              POST /api/bookings/calculate (.NET Core Web API)
            </span>
            <h3 className="text-lg font-extrabold text-slate-900">
              {vehicle.brand} {vehicle.model}
            </h3>
          </div>
          <button
            onClick={onClose}
            className="w-8 h-8 rounded-full bg-slate-100 hover:bg-slate-200 flex items-center justify-center text-slate-600 font-bold"
          >
            ✕
          </button>
        </div>

        <div className="space-y-3 text-sm">
          <div>
            <label className="block font-bold text-slate-700 mb-1">Số ngày thuê:</label>
            <input
              type="number"
              min="1"
              max="30"
              value={days}
              onChange={(e) => setDays(Number(e.target.value) || 1)}
              className="w-full px-3.5 py-2 rounded-xl border border-slate-200"
            />
          </div>

          <div>
            <label className="block font-bold text-slate-700 mb-1">Mã giảm giá (Coupon):</label>
            <select
              value={couponCode}
              onChange={(e) => setCouponCode(e.target.value)}
              className="w-full px-3.5 py-2 rounded-xl border border-slate-200"
            >
              <option value="">-- Không áp dụng mã --</option>
              {coupons.map((c) => (
                <option key={c.code} value={c.code}>
                  {c.code} (-{c.discountPercent}% tối đa {formatVnd(c.maxDiscountAmount)})
                </option>
              ))}
            </select>
          </div>
        </div>

        {quote && (
          <div className="bg-slate-50 rounded-2xl p-4 space-y-2 text-sm border border-slate-200">
            <div className="flex justify-between">
              <span className="text-slate-500">Tiền thuê ({quote.days} ngày):</span>
              <span className="font-bold">{formatVnd(quote.rentalFee)}</span>
            </div>
            <div className="flex justify-between">
              <span className="text-slate-500">Bảo hiểm pin & hành trình:</span>
              <span className="font-bold">{formatVnd(quote.insuranceFee)}</span>
            </div>
            {quote.discountAmount > 0 && (
              <div className="flex justify-between text-emerald-600">
                <span>Giảm giá ({quote.couponCode}):</span>
                <span className="font-bold">-{formatVnd(quote.discountAmount)}</span>
              </div>
            )}
            <div className="flex justify-between pt-2 border-t border-slate-200 text-base font-black text-slate-900">
              <span>Tổng thanh toán:</span>
              <span className="text-emerald-600">{formatVnd(quote.totalAmount)}</span>
            </div>
            <div className="flex justify-between text-xs text-slate-500">
              <span>Tiền cọc giữ xe (30%):</span>
              <span className="font-bold">{formatVnd(quote.depositRequired)}</span>
            </div>
          </div>
        )}

        <div className="flex gap-3">
          <button
            onClick={onClose}
            className="w-1/2 py-2.5 rounded-xl bg-slate-100 hover:bg-slate-200 font-bold text-xs text-slate-700"
          >
            Đóng
          </button>
          <a
            href={`/Vehicles/Details/${vehicle.id}`}
            className="w-1/2 py-2.5 rounded-xl bg-emerald-600 hover:bg-emerald-700 font-bold text-xs text-white text-center"
          >
            Tiến hành đặt xe
          </a>
        </div>
      </div>
    </div>
  );
}

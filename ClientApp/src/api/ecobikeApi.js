/**
 * Tầng gọi RESTful API từ ReactJS sang Backend ASP.NET Core 9.0 Web API
 * Đường dẫn gốc: /api (Endpoints/EcoBikeEndpoints.cs)
 */

const API_BASE = '/api';

export async function fetchVehicles(filters = {}) {
  const params = new URLSearchParams();
  if (filters.category !== undefined && filters.category !== '') params.append('category', filters.category);
  if (filters.city) params.append('city', filters.city);
  if (filters.brand) params.append('brand', filters.brand);
  if (filters.requiresLicense !== undefined && filters.requiresLicense !== '') {
    params.append('requiresLicense', filters.requiresLicense);
  }
  if (filters.maxPrice) params.append('maxPrice', filters.maxPrice);
  if (filters.search) params.append('search', filters.search);

  const response = await fetch(`${API_BASE}/vehicles?${params.toString()}`);
  if (!response.ok) throw new Error('Không thể tải danh sách xe máy điện từ .NET Core API');
  return response.json();
}

export async function fetchVehicleById(id) {
  const response = await fetch(`${API_BASE}/vehicles/${id}`);
  if (!response.ok) throw new Error('Không tìm thấy thông tin xe');
  return response.json();
}

export async function fetchSystemStats() {
  const response = await fetch(`${API_BASE}/stats`);
  if (!response.ok) throw new Error('Không thể tải thống kê hệ thống');
  return response.json();
}

export async function fetchCoupons() {
  const response = await fetch(`${API_BASE}/coupons`);
  if (!response.ok) throw new Error('Không thể tải danh sách mã giảm giá');
  return response.json();
}

export async function fetchStations() {
  const response = await fetch(`${API_BASE}/stations`);
  if (!response.ok) throw new Error('Không thể tải danh sách trạm sạc');
  return response.json();
}

export async function calculateBookingQuote(vehicleId, days, couponCode = '') {
  const params = new URLSearchParams({
    vehicleId: String(vehicleId),
    days: String(days),
    couponCode: couponCode || ''
  });
  const response = await fetch(`${API_BASE}/bookings/calculate?${params.toString()}`, {
    method: 'POST'
  });
  if (!response.ok) throw new Error('Không thể tính phí thuê xe');
  return response.json();
}

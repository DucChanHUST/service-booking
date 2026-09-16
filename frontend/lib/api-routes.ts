type QueryValue = string | number | undefined;
type QueryParams = Record<string, QueryValue>;

function withQuery(path: string, params: QueryParams) {
  const query = new URLSearchParams();

  Object.entries(params).forEach(([key, value]) => {
    if (value !== undefined && value !== "") {
      query.set(key, String(value));
    }
  });

  const queryString = query.toString();

  return queryString ? `${path}?${queryString}` : path;
}

export const apiRoutes = {
  auth: {
    login: "/auth/login",
  },
  services: {
    list: (params: QueryParams = {}) => withQuery("/services", params),
    detail: (serviceId: string) => `/services/${serviceId}`,
    create: "/services",
    update: (serviceId: string) => `/services/${serviceId}`,
  },
  staffs: {
    list: "/staffs",
    create: "/staffs",
    update: (staffId: string) => `/staffs/${staffId}`,
  },
  bookings: {
    list: (params: QueryParams = {}) => withQuery("/bookings", params),
    mine: (params: QueryParams = {}) =>
      withQuery("/bookings/my-bookings", params),
    availableSlots: (params: QueryParams) =>
      withQuery("/bookings/available-slots", params),
    create: "/bookings",
    updateStatus: (bookingId: string) => `/bookings/${bookingId}/status`,
    cancel: (bookingId: string) => `/bookings/${bookingId}/cancel`,
  },
  schedules: {
    list: (staffId: string, params: QueryParams = {}) =>
      withQuery(`/staffs/${staffId}/schedules`, params),
    create: (staffId: string) => `/staffs/${staffId}/schedules`,
    update: (scheduleId: string) => `/staffs/schedules/${scheduleId}`,
    delete: (scheduleId: string) => `/staffs/schedules/${scheduleId}`,
  },
} as const;

export type BookingStatus = "Pending" | "Confirmed" | "Completed" | "Cancelled";

export interface AvailableRange {
  startTime: string;
  endTime: string;
}

export interface AvailableSlots {
  serviceId: string;
  staffId: string;
  date: string;
  durationMinutes: number;
  availableRanges: AvailableRange[];
}

export interface CreateBookingRequest {
  serviceId: string;
  staffId: string;
  startTime: string;
  customerNote?: string;
}

export interface Booking {
  id: string;
  bookingCode: string;
  customerId: string;
  customerEmail: string;
  serviceId: string;
  serviceName: string;
  staffId: string;
  staffName: string;
  startTime: string;
  endTime: string;
  status: BookingStatus;
  customerNote?: string | null;
  cancellationReason?: string | null;
  createdAt: string;
}

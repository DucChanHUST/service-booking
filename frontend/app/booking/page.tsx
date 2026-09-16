"use client";

import { useEffect, useMemo, useState } from "react";
import { useSearchParams } from "next/navigation";

import { api } from "@/lib/api";
import { apiRoutes } from "@/lib/api-routes";

import type { Service } from "@/types/service";
import type { Staff } from "@/types/staff";
import type { AvailableSlots, CreateBookingRequest } from "@/types/booking";

import AuthGuard from "@/components/auth/AuthGuard";

function calculateEndTime(startTime: string, durationMinutes: number) {
  const [hours, minutes] = startTime.split(":").map(Number);

  const totalMinutes = hours * 60 + minutes + durationMinutes;

  const endHours = Math.floor(totalMinutes / 60);
  const endMinutes = totalMinutes % 60;

  return `${String(endHours).padStart(2, "0")}:${String(endMinutes).padStart(
    2,
    "0",
  )}`;
}

function timeToMinutes(time: string) {
  const [hours, minutes] = time.slice(0, 5).split(":").map(Number);

  return hours * 60 + minutes;
}

function getTimelinePosition(minutes: number) {
  return `${(Math.max(0, Math.min(minutes, 24 * 60)) / (24 * 60)) * 100}%`;
}

function isTimeAvailable(
  startTime: string,
  durationMinutes: number,
  ranges: AvailableSlots["availableRanges"],
) {
  const start = timeToMinutes(startTime);
  const end = start + durationMinutes;

  return ranges.some((range) => {
    const rangeStart = timeToMinutes(range.startTime);

    const rangeEnd = timeToMinutes(range.endTime);

    return start >= rangeStart && end <= rangeEnd;
  });
}

function BookingPageContent() {
  const searchParams = useSearchParams();

  const serviceId = searchParams.get("serviceId");

  const [service, setService] = useState<Service | null>(null);

  const [staffs, setStaffs] = useState<Staff[]>([]);

  const [staffId, setStaffId] = useState("");

  const [date, setDate] = useState("");

  const [startTime, setStartTime] = useState("");

  const [customerNote, setCustomerNote] = useState("");

  const [availability, setAvailability] = useState<AvailableSlots | null>(null);

  const [loading, setLoading] = useState(true);

  const [loadingAvailability, setLoadingAvailability] = useState(false);

  const [submitting, setSubmitting] = useState(false);

  const [error, setError] = useState("");

  const [success, setSuccess] = useState("");

  /*
   * Load service + staff
   */
  useEffect(() => {
    if (!serviceId) {
      setError("Service ID is missing.");
      setLoading(false);
      return;
    }

    const selectedServiceId = serviceId;

    async function loadData() {
      try {
        setLoading(true);
        setError("");

        const [serviceData, staffData] = await Promise.all([
          api.get<Service>(apiRoutes.services.detail(selectedServiceId)),
          api.get<Staff[]>(apiRoutes.staffs.list),
        ]);

        setService(serviceData);

        setStaffs(staffData.filter((staff) => staff.isActive));
      } catch (err) {
        setError(
          err instanceof Error ? err.message : "Failed to load booking data.",
        );
      } finally {
        setLoading(false);
      }
    }

    loadData();
  }, [serviceId]);

  /*
   * Load availability
   */
  useEffect(() => {
    if (!serviceId || !staffId || !date) {
      setAvailability(null);
      setStartTime("");
      return;
    }

    const selectedServiceId = serviceId;

    async function loadAvailability() {
      try {
        setLoadingAvailability(true);
        setError("");
        setStartTime("");

        const data = await api.get<AvailableSlots>(
          apiRoutes.bookings.availableSlots({
            serviceId: selectedServiceId,
            staffId,
            date,
          }),
        );

        setAvailability(data);
      } catch (err) {
        setAvailability(null);

        setError(
          err instanceof Error ? err.message : "Failed to load availability.",
        );
      } finally {
        setLoadingAvailability(false);
      }
    }

    loadAvailability();
  }, [serviceId, staffId, date]);

  const endTime = useMemo(() => {
    if (!service || !startTime) {
      return "";
    }

    return calculateEndTime(startTime, service.durationMinutes);
  }, [service, startTime]);

  const selectedStartMinutes = startTime ? timeToMinutes(startTime) : null;
  const selectedEndMinutes = endTime ? timeToMinutes(endTime) : null;

  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    setError("");
    setSuccess("");

    if (!serviceId || !service) {
      setError("Service is not available.");
      return;
    }

    if (!staffId) {
      setError("Please select a staff member.");
      return;
    }

    if (!date) {
      setError("Please select a date.");
      return;
    }

    if (!startTime) {
      setError("Please select a start time.");
      return;
    }

    if (!availability) {
      setError("Availability has not been loaded.");
      return;
    }

    const valid = isTimeAvailable(
      startTime,
      service.durationMinutes,
      availability.availableRanges,
    );

    if (!valid) {
      setError(
        "The selected time is not available for the full service duration.",
      );
      return;
    }

    const request: CreateBookingRequest = {
      serviceId,
      staffId,
      startTime: `${date}T${startTime}:00`,
      customerNote: customerNote.trim() || undefined,
    };

    try {
      setSubmitting(true);

      const result = await api.post<{
        bookingCode: string;
      }>(apiRoutes.bookings.create, request);

      setSuccess(`Booking created successfully. Code: ${result.bookingCode}`);

      setStartTime("");
      setCustomerNote("");

      /*
       * Refresh availability
       */
      const updated = await api.get<AvailableSlots>(
        apiRoutes.bookings.availableSlots({ serviceId, staffId, date }),
      );

      setAvailability(updated);
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to create booking.",
      );
    } finally {
      setSubmitting(false);
    }
  }

  if (loading) {
    return (
      <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="mb-10 max-w-2xl animate-pulse">
          <div className="h-3 w-24 rounded bg-gray-200" />
          <div className="mt-4 h-10 w-80 rounded bg-gray-200" />
          <div className="mt-4 h-5 w-full max-w-xl rounded bg-gray-100" />
        </div>
        <div className="grid gap-6 lg:grid-cols-[minmax(260px,0.8fr)_minmax(0,1.4fr)]">
          <div className="h-64 animate-pulse rounded-2xl border border-(--border) bg-white p-6">
            <div className="h-5 w-2/3 rounded bg-gray-200" />
            <div className="mt-5 h-12 rounded bg-gray-100" />
            <div className="mt-10 h-5 w-1/2 rounded bg-gray-200" />
          </div>
          <div className="h-120 animate-pulse rounded-2xl border border-(--border) bg-white p-6">
            <div className="h-5 w-1/3 rounded bg-gray-200" />
            <div className="mt-8 grid gap-4 sm:grid-cols-2">
              <div className="h-12 rounded bg-gray-100" />
              <div className="h-12 rounded bg-gray-100" />
            </div>
          </div>
        </div>
      </main>
    );
  }

  if (!service) {
    return (
      <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
        <div className="rounded-2xl border border-red-200 bg-red-50 p-6 text-red-800">
          <p className="text-sm font-semibold">
            We couldn&apos;t open this service
          </p>
          <p className="mt-1 text-sm text-red-700">
            {error || "Service not found."}
          </p>
        </div>
      </main>
    );
  }

  return (
    <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 sm:py-14 lg:px-8">
      <header className="mb-10 max-w-2xl">
        <p className="text-sm font-bold uppercase tracking-[0.16em] text-(--brand)">
          Reserve your time
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-foreground sm:text-5xl">
          Book a service.
        </h1>
        <p className="mt-4 text-base leading-7 text-(--muted)">
          Choose a team member, find an available time, and we&apos;ll take care
          of the rest.
        </p>
      </header>

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(260px,0.8fr)_minmax(0,1.4fr)]">
        <aside className="overflow-hidden rounded-2xl border border-(--border) bg-white">
          <div className="h-1.5 bg-(--brand)" />
          <div className="p-6 sm:p-8">
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
              Selected service
            </p>
            <h2 className="mt-4 text-2xl font-bold tracking-tight text-foreground">
              {service.name}
            </h2>
            <p className="mt-4 text-sm leading-6 text-(--muted)">
              {service.description ||
                "A focused session tailored to your needs."}
            </p>

            <div className="mt-8 grid grid-cols-2 gap-4 border-t border-(--border) pt-6">
              <div>
                <p className="text-xs font-semibold uppercase tracking-wider text-gray-400">
                  Duration
                </p>
                <p className="mt-2 font-semibold text-foreground">
                  {service.durationMinutes} minutes
                </p>
              </div>
              <div>
                <p className="text-xs font-semibold uppercase tracking-wider text-gray-400">
                  Price
                </p>
                <p className="mt-2 font-semibold text-foreground">
                  {service.price.toLocaleString("vi-VN")}₫
                </p>
              </div>
            </div>

            <div className="mt-8 rounded-xl bg-[#f5faf8] p-4">
              <p className="text-sm font-semibold text-(--brand-dark)">
                A little planning goes a long way
              </p>
              <p className="mt-1 text-sm leading-5 text-(--muted)">
                Your selected time will be reserved for the full service
                duration.
              </p>
            </div>
          </div>
        </aside>

        <section className="rounded-2xl border border-(--border) bg-white p-6 sm:p-8">
          <div className="mb-8">
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
              Booking details
            </p>
            <h2 className="mt-2 text-2xl font-bold tracking-tight text-foreground">
              Choose when to meet
            </h2>
          </div>

          {error && (
            <div
              className="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-red-800"
              role="alert"
            >
              <p className="text-sm font-semibold">
                Something needs your attention
              </p>
              <p className="mt-1 text-sm text-red-700">{error}</p>
            </div>
          )}

          {success && (
            <div
              className="mb-6 rounded-xl border border-green-200 bg-green-50 p-4 text-green-800"
              role="status"
            >
              <p className="text-sm font-semibold">Booking confirmed</p>
              <p className="mt-1 text-sm text-green-700">{success}</p>
            </div>
          )}

          <form onSubmit={handleSubmit} className="space-y-7">
            <div className="grid gap-5 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="staff"
                  className="mb-2 block text-sm font-semibold text-foreground"
                >
                  Staff member
                </label>
                <select
                  id="staff"
                  value={staffId}
                  onChange={(event) => setStaffId(event.target.value)}
                  className="w-full rounded-xl border border-(--border) bg-white px-4 py-3 text-sm text-foreground outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                >
                  <option value="">Select a staff member</option>
                  {staffs.map((staff) => (
                    <option key={staff.id} value={staff.id}>
                      {staff.fullName}
                    </option>
                  ))}
                </select>
              </div>

              <div>
                <label
                  htmlFor="date"
                  className="mb-2 block text-sm font-semibold text-foreground"
                >
                  Date
                </label>
                <input
                  id="date"
                  type="date"
                  value={date}
                  min={new Date().toISOString().slice(0, 10)}
                  onChange={(event) => setDate(event.target.value)}
                  className="w-full rounded-xl border border-(--border) bg-white px-4 py-3 text-sm text-foreground outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                />
              </div>
            </div>

            <div className="border-t border-(--border) pt-7">
              <div className="flex flex-col justify-between gap-2 sm:flex-row sm:items-end">
                <div>
                  <label
                    htmlFor="startTime"
                    className="block text-sm font-semibold text-foreground"
                  >
                    Available time
                  </label>
                  <p className="mt-1 text-sm text-(--muted)">
                    Select a start time that fits your schedule.
                  </p>
                </div>
                {endTime && (
                  <p className="text-sm font-semibold text-(--brand-dark)">
                    Ends at {endTime}
                  </p>
                )}
              </div>

              {loadingAvailability && (
                <div
                  className="mt-4 flex items-center gap-3 rounded-xl bg-gray-50 p-4 text-sm text-(--muted)"
                  role="status"
                >
                  <span className="h-4 w-4 animate-spin rounded-full border-2 border-gray-300 border-t-(--brand)" />
                  Checking availability...
                </div>
              )}

              {availability && !loadingAvailability && (
                <div className="mt-4 rounded-xl bg-gray-50 p-4">
                  {availability.availableRanges.length === 0 ? (
                    <p className="text-sm text-(--muted)">
                      No available times for this staff member on the selected
                      date.
                    </p>
                  ) : (
                    <div>
                      <div className="flex items-center justify-between gap-3">
                        <p className="text-xs font-bold uppercase tracking-wider text-gray-400">
                          Availability timeline
                        </p>
                        <div className="flex shrink-0 items-center gap-3 text-[11px] text-(--muted)">
                          <span className="flex items-center gap-1.5">
                            <span className="h-2.5 w-2.5 rounded-sm bg-gray-300" />
                            Unavailable
                          </span>
                          <span className="flex items-center gap-1.5">
                            <span className="h-2.5 w-2.5 rounded-sm bg-emerald-500" />
                            Available
                          </span>
                          <span className="flex items-center gap-1.5">
                            <span className="h-2.5 w-2.5 rounded-sm bg-red-500" />
                            Selected
                          </span>
                        </div>
                      </div>

                      <div className="mt-4 overflow-hidden rounded-lg border border-gray-200 bg-gray-200">
                        <div className="relative h-16 bg-[linear-gradient(to_right,rgba(255,255,255,0.65)_1px,transparent_1px)] bg-[length:calc(100%/12)_100%]">
                          {availability.availableRanges.map((range, index) => {
                            const rangeStart = timeToMinutes(range.startTime);
                            const rangeEnd = timeToMinutes(range.endTime);

                            return (
                              <div
                                key={index}
                                className="absolute inset-y-0 bg-emerald-500/85"
                                style={{
                                  left: getTimelinePosition(rangeStart),
                                  width: `${(Math.max(0, rangeEnd - rangeStart) / (24 * 60)) * 100}%`,
                                }}
                                title={`${range.startTime.slice(0, 5)} - ${range.endTime.slice(0, 5)} available`}
                              />
                            );
                          })}

                          {selectedStartMinutes !== null &&
                            selectedEndMinutes !== null && (
                              <div
                                className="absolute inset-y-0 z-10 border-2 border-red-600 bg-red-500/75 shadow-sm"
                                style={{
                                  left: getTimelinePosition(
                                    selectedStartMinutes,
                                  ),
                                  width: `${(Math.max(0, Math.min(selectedEndMinutes, 24 * 60) - Math.max(selectedStartMinutes, 0)) / (24 * 60)) * 100}%`,
                                }}
                                title={`Selected service: ${startTime} - ${endTime}`}
                              />
                            )}
                        </div>
                        <div className="flex justify-between px-1 py-1 text-[10px] text-gray-500">
                          <span>00:00</span>
                          <span>06:00</span>
                          <span>12:00</span>
                          <span>18:00</span>
                          <span>24:00</span>
                        </div>
                      </div>

                      <div className="mt-3 space-y-1 text-xs text-(--muted)">
                        {availability.availableRanges.map((range, index) => (
                          <p key={index}>
                            Available: {range.startTime.slice(0, 5)} -{" "}
                            {range.endTime.slice(0, 5)}
                          </p>
                        ))}
                      </div>
                    </div>
                  )}
                </div>
              )}

              <input
                id="startTime"
                type="time"
                value={startTime}
                disabled={
                  !availability || availability.availableRanges.length === 0
                }
                onChange={(event) => setStartTime(event.target.value)}
                className="mt-4 w-full rounded-xl border border-(--border) bg-white px-4 py-3 text-sm text-foreground outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed] disabled:cursor-not-allowed disabled:bg-gray-50 disabled:text-gray-400"
              />
            </div>

            <div>
              <label
                htmlFor="customerNote"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Note{" "}
                <span className="font-normal text-(--muted)">(optional)</span>
              </label>
              <textarea
                id="customerNote"
                rows={4}
                maxLength={1000}
                value={customerNote}
                onChange={(event) => setCustomerNote(event.target.value)}
                placeholder="Anything we should know before your appointment?"
                className="w-full resize-y rounded-xl border border-(--border) px-4 py-3 text-sm text-foreground outline-none transition placeholder:text-gray-400 focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
              />
              <p className="mt-2 text-right text-xs text-gray-400">
                {customerNote.length}/1000
              </p>
            </div>

            <button
              type="submit"
              disabled={submitting}
              className="w-full rounded-xl bg-(--brand) px-5 py-3.5 text-sm font-bold text-white transition hover:bg-(--brand-dark) disabled:cursor-not-allowed disabled:opacity-50"
            >
              {submitting ? "Creating booking..." : "Confirm booking"}
            </button>
          </form>
        </section>
      </div>
    </main>
  );
}

export default function BookingPage() {
  return (
    <AuthGuard requiredRole="Customer">
      <BookingPageContent />
    </AuthGuard>
  );
}

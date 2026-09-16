"use client";

import React, { useEffect, useState } from "react";
import { api } from "@/lib/api";
import { apiRoutes } from "@/lib/api-routes";
import type { Staff } from "@/types/staff";
import type {
  CreateScheduleRequest,
  UpdateScheduleRequest,
} from "@/types/schedule";

import AuthGuard from "@/components/auth/AuthGuard";

interface Schedule {
  id: string;
  staffId: string;
  staffName: string;
  workDate: string;
  startTime: string;
  endTime: string;
}

interface ScheduleResponse {
  items?: Schedule[];
}

function getToday() {
  return new Date().toISOString().slice(0, 10);
}

function parseDate(value: string) {
  const [year, month, day] = value.split("-").map(Number);

  return new Date(year, month - 1, day);
}

function formatDate(value: Date) {
  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, "0");
  const day = String(value.getDate()).padStart(2, "0");

  return `${year}-${month}-${day}`;
}

function getWeekDays(value: string) {
  const date = parseDate(value);
  const dayOfWeek = date.getDay();
  const mondayOffset = dayOfWeek === 0 ? -6 : 1 - dayOfWeek;

  date.setDate(date.getDate() + mondayOffset);

  return Array.from({ length: 7 }, (_, index) => {
    const weekDay = new Date(date);
    weekDay.setDate(date.getDate() + index);

    return {
      date: formatDate(weekDay),
      label: weekDay.toLocaleDateString("en-US", { weekday: "short" }),
      day: weekDay.getDate(),
    };
  });
}

function timeToMinutes(value: string) {
  const [hours, minutes] = value.slice(0, 5).split(":").map(Number);

  return hours * 60 + minutes;
}

function AdminSchedulesPageContent() {
  const [staffs, setStaffs] = useState<Staff[]>([]);
  const [schedules, setSchedules] = useState<Schedule[]>([]);

  const [selectedStaffId, setSelectedStaffId] = useState("");
  const [selectedDate, setSelectedDate] = useState(getToday());
  const [calendarDate, setCalendarDate] = useState(getToday());

  const [startTime, setStartTime] = useState("08:00");
  const [endTime, setEndTime] = useState("12:00");

  const [editingId, setEditingId] = useState<string | null>(null);

  const [loadingStaffs, setLoadingStaffs] = useState(true);
  const [loadingSchedules, setLoadingSchedules] = useState(false);
  const [saving, setSaving] = useState(false);

  const [error, setError] = useState("");

  async function loadStaffs() {
    try {
      setLoadingStaffs(true);
      setError("");

      const result = await api.get<Staff[]>(apiRoutes.staffs.list);

      setStaffs(result);

      if (result.length > 0 && !selectedStaffId) {
        setSelectedStaffId(result[0].id);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to load staffs.");
    } finally {
      setLoadingStaffs(false);
    }
  }

  async function loadSchedules() {
    if (!selectedStaffId || !selectedDate) {
      setSchedules([]);
      return;
    }

    try {
      setLoadingSchedules(true);
      setError("");

      const weekDays = getWeekDays(calendarDate);

      const result = await api.get<ScheduleResponse | Schedule[]>(
        apiRoutes.schedules.list(selectedStaffId, {
          from: weekDays[0].date,
          to: weekDays[6].date,
        }),
      );

      const items = Array.isArray(result) ? result : (result.items ?? []);

      setSchedules(
        [...items].sort((a, b) => a.startTime.localeCompare(b.startTime)),
      );
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to load schedules.",
      );
    } finally {
      setLoadingSchedules(false);
    }
  }

  useEffect(() => {
    loadStaffs();
  }, []);

  useEffect(() => {
    loadSchedules();
  }, [selectedStaffId, calendarDate]);

  function resetForm() {
    setEditingId(null);
    setStartTime("08:00");
    setEndTime("12:00");
  }

  function startEdit(schedule: Schedule) {
    setEditingId(schedule.id);

    setSelectedDate(schedule.workDate);
    setStartTime(schedule.startTime.slice(0, 5));
    setEndTime(schedule.endTime.slice(0, 5));
  }

  async function handleSubmit(event: React.SubmitEvent<HTMLFormElement>) {
    event.preventDefault();

    setError("");

    if (!selectedStaffId) {
      setError("Please select a staff member.");
      return;
    }

    if (!selectedDate) {
      setError("Please select a work date.");
      return;
    }

    if (startTime >= endTime) {
      setError("Start time must be earlier than end time.");
      return;
    }

    try {
      setSaving(true);

      if (editingId) {
        const body: UpdateScheduleRequest = {
          workDate: selectedDate,
          startTime,
          endTime,
        };

        await api.put(apiRoutes.schedules.update(editingId), body);
      } else {
        const body: CreateScheduleRequest = {
          workDate: selectedDate,
          startTime,
          endTime,
        };

        await api.post(apiRoutes.schedules.create(selectedStaffId), body);
      }

      resetForm();

      await loadSchedules();
    } catch (err) {
      setError(err instanceof Error ? err.message : "Failed to save schedule.");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(schedule: Schedule) {
    const confirmed = window.confirm(
      `Delete ${schedule.startTime.slice(0, 5)} - ${schedule.endTime.slice(0, 5)}?`,
    );

    if (!confirmed) {
      return;
    }

    try {
      setError("");

      await api.delete(apiRoutes.schedules.delete(schedule.id));

      if (editingId === schedule.id) {
        resetForm();
      }

      await loadSchedules();
    } catch (err) {
      setError(
        err instanceof Error ? err.message : "Failed to delete schedule.",
      );
    }
  }

  const selectedStaff = staffs.find((staff) => staff.id === selectedStaffId);
  const weekDays = getWeekDays(calendarDate);
  const hourLabels = Array.from({ length: 24 }, (_, hour) => hour);

  return (
    <main className="mx-auto max-w-7xl px-4 py-10 sm:px-6 lg:px-8">
      <header className="mb-8">
        <p className="text-sm font-bold uppercase tracking-[0.16em] text-(--brand)">
          Admin workspace
        </p>
        <h1 className="mt-3 text-4xl font-bold tracking-tight text-foreground">
          Schedule management
        </h1>
        <p className="mt-3 text-base leading-7 text-(--muted)">
          Set working hours so customers can see accurate availability.
        </p>
      </header>

      {error && (
        <div
          className="mb-6 rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-700"
          role="alert"
        >
          {error}
        </div>
      )}

      <div className="grid items-start gap-6 lg:grid-cols-[minmax(280px,0.75fr)_minmax(0,1.25fr)]">
        <section className="rounded-2xl border border-(--border) bg-white p-6 sm:p-8">
          <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
            Working hours
          </p>
          <h2 className="mt-2 text-2xl font-bold tracking-tight text-foreground">
            {editingId ? "Edit schedule" : "Create schedule"}
          </h2>
          <form onSubmit={handleSubmit} className="mt-6 space-y-5">
            <div>
              <label
                htmlFor="schedule-staff"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Staff member
              </label>
              <select
                id="schedule-staff"
                value={selectedStaffId}
                disabled={loadingStaffs || Boolean(editingId)}
                onChange={(event) => setSelectedStaffId(event.target.value)}
                className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed] disabled:bg-gray-50"
              >
                {staffs.map((staff) => (
                  <option key={staff.id} value={staff.id}>
                    {staff.fullName} — {staff.email}
                    {!staff.isActive ? " (Inactive)" : ""}
                  </option>
                ))}
              </select>
            </div>
            <div>
              <label
                htmlFor="schedule-date"
                className="mb-2 block text-sm font-semibold text-foreground"
              >
                Work date
              </label>
              <input
                id="schedule-date"
                type="date"
                lang="vi-VN"
                value={selectedDate}
                onChange={(event) => {
                  setSelectedDate(event.target.value);
                  setCalendarDate(event.target.value);
                }}
                className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
              />
            </div>
            <div className="grid gap-4 sm:grid-cols-2">
              <div>
                <label
                  htmlFor="schedule-start"
                  className="mb-2 block text-sm font-semibold text-foreground"
                >
                  Start time
                </label>
                <input
                  id="schedule-start"
                  type="time"
                  value={startTime}
                  onChange={(event) => setStartTime(event.target.value)}
                  className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                />
              </div>
              <div>
                <label
                  htmlFor="schedule-end"
                  className="mb-2 block text-sm font-semibold text-foreground"
                >
                  End time
                </label>
                <input
                  id="schedule-end"
                  type="time"
                  value={endTime}
                  onChange={(event) => setEndTime(event.target.value)}
                  className="w-full rounded-xl border border-(--border) px-4 py-3 text-sm outline-none transition focus:border-(--brand) focus:ring-4 focus:ring-[#e1f1ed]"
                />
              </div>
            </div>
            <div className="flex flex-wrap gap-3 pt-2">
              <button
                type="submit"
                disabled={saving}
                className="rounded-xl bg-(--brand) px-4 py-3 text-sm font-bold text-white transition hover:bg-(--brand-dark) disabled:cursor-not-allowed disabled:opacity-50"
              >
                {saving
                  ? "Saving..."
                  : editingId
                    ? "Update schedule"
                    : "Create schedule"}
              </button>
              {editingId && (
                <button
                  type="button"
                  onClick={resetForm}
                  className="rounded-xl border border-(--border) px-4 py-3 text-sm font-semibold text-foreground hover:bg-gray-50"
                >
                  Cancel
                </button>
              )}
            </div>
          </form>
        </section>

        <section className="min-w-0 rounded-2xl border border-(--border) bg-white p-6 sm:p-8">
          <div className="border-b border-(--border) pb-6">
            <p className="text-xs font-bold uppercase tracking-[0.16em] text-(--muted)">
              Weekly timetable
            </p>
            <h2 className="mt-2 text-2xl font-bold tracking-tight text-foreground">
              Work schedule
            </h2>
            {selectedStaff && (
              <p className="mt-2 text-sm text-(--muted)">
                {selectedStaff.fullName} · {weekDays[0].date} to{" "}
                {weekDays[6].date}
              </p>
            )}
          </div>
          {loadingSchedules ? (
            <div className="mt-6 space-y-3">
              <div className="h-20 animate-pulse rounded-xl bg-gray-100" />
              <div className="h-20 animate-pulse rounded-xl bg-gray-100" />
            </div>
          ) : schedules.length === 0 ? (
            <div className="mt-6 rounded-xl border border-dashed border-gray-300 px-6 py-12 text-center">
              <p className="font-semibold text-foreground">
                No schedules this week
              </p>
              <p className="mt-1 text-sm text-(--muted)">
                Create a working window to make a day bookable.
              </p>
            </div>
          ) : (
            <div className="mt-6 pb-2">
              <div>
                <div className="grid grid-cols-[40px_repeat(7,minmax(0,1fr))] border-b border-(--border) pb-2 text-[10px] text-gray-400">
                  <div />
                  {weekDays.map((weekDay) => (
                    <div key={weekDay.date} className="text-center">
                      <p className="font-bold text-foreground">
                        {weekDay.label}
                      </p>
                      <p className="mt-1">{weekDay.day}</p>
                    </div>
                  ))}
                </div>
                <div className="grid grid-cols-[40px_minmax(0,1fr)]">
                  <div className="grid grid-rows-[repeat(24,40px)] border-r border-gray-100 text-[10px] text-gray-400">
                    {hourLabels.map((hour) => (
                      <div key={hour} className="pr-2 pt-1 text-right">
                        {String(hour).padStart(2, "0")}:00
                      </div>
                    ))}
                  </div>
                  <div
                    className="relative h-[960px] grid-cols-7 bg-[linear-gradient(to_bottom,#f3f4f6_1px,transparent_1px),linear-gradient(to_right,#f3f4f6_1px,transparent_1px)] bg-[length:100%_40px,calc(100%/7)_100%]"
                    style={{ display: "grid" }}
                  >
                    {schedules.map((schedule) => {
                      const dayIndex = weekDays.findIndex(
                        (weekDay) => weekDay.date === schedule.workDate,
                      );
                      const start = timeToMinutes(schedule.startTime);
                      const end = timeToMinutes(schedule.endTime);
                      const duration = Math.max(end - start, 15);

                      if (dayIndex < 0) {
                        return null;
                      }

                      return (
                        <button
                          key={schedule.id}
                          type="button"
                          onClick={() => startEdit(schedule)}
                          title={`${schedule.startTime.slice(0, 5)} - ${schedule.endTime.slice(0, 5)}`}
                          className="absolute rounded-md border border-(--brand) bg-(--brand) px-2 py-1 text-left text-[10px] font-bold text-white shadow-sm transition hover:bg-(--brand-dark)"
                          style={{
                            left: `${(dayIndex / 7) * 100}%`,
                            top: `${(start / 1440) * 100}%`,
                            width: `${100 / 7}%`,
                            height: `${(duration / 1440) * 100}%`,
                          }}
                        >
                          <span className="block truncate">
                            {schedule.startTime.slice(0, 5)}
                          </span>
                          <span className="block truncate font-normal">
                            {schedule.endTime.slice(0, 5)}
                          </span>
                        </button>
                      );
                    })}
                  </div>
                </div>
              </div>
            </div>
          )}
        </section>
      </div>
    </main>
  );
}

export default function AdminSchedulesPage() {
  return (
    <AuthGuard requiredRole="Admin">
      <AdminSchedulesPageContent />
    </AuthGuard>
  );
}

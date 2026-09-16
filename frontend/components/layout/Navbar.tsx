"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { useAuth } from "@/components/auth/AuthProvider";

export default function Navbar() {
  const router = useRouter();
  const pathname = usePathname();
  const { user, isLoading, logout } = useAuth();

  function isActive(href: string) {
    return (
      pathname === href || (href !== "/" && pathname.startsWith(`${href}/`))
    );
  }

  function getLinkClass(href: string) {
    return `whitespace-nowrap rounded-lg px-3 py-2 text-sm font-semibold transition ${
      isActive(href)
        ? "bg-[#edf7f4] text-(--brand-dark)"
        : "text-(--muted) hover:bg-gray-100 hover:text-foreground"
    }`;
  }

  function handleLogout() {
    logout();
    router.push("/login");
    router.refresh();
  }

  if (isLoading) {
    return null;
  }

  return (
    <nav
      className="border-b border-(--border) bg-white/95 backdrop-blur"
      aria-label="Main navigation"
    >
      <div className="mx-auto flex max-w-7xl flex-wrap items-center justify-between gap-x-6 gap-y-4 px-4 py-3 sm:px-6 lg:px-8">
        <div className="flex items-center gap-8">
          <Link
            href="/"
            className="group flex items-center gap-3 text-sm font-bold tracking-tight text-foreground"
          >
            <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-(--brand) text-sm font-bold text-white shadow-[0_5px_14px_rgba(23,107,91,0.2)] transition group-hover:scale-105">
              SB
            </span>
            <span className="hidden sm:inline">Service Booking</span>
          </Link>

          {user && (
            <div className="order-3 flex w-full items-center gap-1 overflow-x-auto pb-0.5 md:order-0 md:w-auto">
              {user.role === "Customer" && (
                <>
                  <Link href="/services" className={getLinkClass("/services")}>
                    Services
                  </Link>

                  <Link href="/booking" className={getLinkClass("/booking")}>
                    Book
                  </Link>

                  <Link
                    href="/my-bookings"
                    className={getLinkClass("/my-bookings")}
                  >
                    My Bookings
                  </Link>
                </>
              )}

              {user.role === "Admin" && (
                <>
                  <Link
                    href="/admin/services"
                    className={getLinkClass("/admin/services")}
                  >
                    Services
                  </Link>

                  <Link
                    href="/admin/schedules"
                    className={getLinkClass("/admin/schedules")}
                  >
                    Schedules
                  </Link>

                  <Link
                    href="/admin/bookings"
                    className={getLinkClass("/admin/bookings")}
                  >
                    Bookings
                  </Link>
                </>
              )}
            </div>
          )}
        </div>

        <div className="flex items-center gap-2">
          {user ? (
            <>
              <div className="hidden items-center gap-2.5 rounded-xl bg-gray-50 py-1.5 pl-1.5 pr-3 sm:flex">
                <span className="flex h-7 w-7 items-center justify-center rounded-lg bg-[#dcefe9] text-xs font-bold text-(--brand-dark)">
                  {user.email.slice(0, 1).toUpperCase()}
                </span>
                <span className="max-w-44 truncate text-sm font-medium text-(--muted)">
                  {user.email}
                </span>
              </div>

              <button
                type="button"
                onClick={handleLogout}
                className="rounded-lg border border-(--border) px-3 py-2 text-sm font-semibold text-foreground transition hover:border-gray-300 hover:bg-gray-50"
              >
                Logout
              </button>
            </>
          ) : (
            <Link
              href="/login"
              className="rounded-lg bg-(--brand) px-4 py-2 text-sm font-semibold text-white transition hover:bg-(--brand-dark)"
            >
              Login
            </Link>
          )}
        </div>
      </div>
    </nav>
  );
}

"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { getCurrentUser, getToken } from "@/lib/auth";
import type { UserRole } from "@/types/auth";

interface AuthGuardProps {
  children: React.ReactNode;
  requiredRole?: UserRole;
}

export default function AuthGuard({ children, requiredRole }: AuthGuardProps) {
  const router = useRouter();

  const [authorized, setAuthorized] = useState(false);

  useEffect(() => {
    const token = getToken();
    const user = getCurrentUser();

    if (!token || !user) {
      router.replace("/login");
      return;
    }

    if (requiredRole && user.role !== requiredRole) {
      router.replace(user.role === "Admin" ? "/admin/services" : "/services");
      return;
    }

    setAuthorized(true);
  }, [requiredRole, router]);

  if (!authorized) {
    return null;
  }

  return <>{children}</>;
}

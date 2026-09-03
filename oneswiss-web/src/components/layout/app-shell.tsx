"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import { ChevronLeft, ChevronRight, LogOut, Menu, X } from "lucide-react";
import { type ReactNode, Suspense, useEffect, useMemo, useState } from "react";

import { AgentsNavTree } from "@/components/layout/agents-nav-tree";
import { OneSwissLogo } from "@/components/layout/oneswiss-logo";
import { ThemeToggle } from "@/components/theme/theme-toggle";
import { Button } from "@/components/ui/button";
import { Separator } from "@/components/ui/separator";
import { ApiError } from "@/lib/api/client";
import { getCurrentUser, logout, type AuthUser } from "@/lib/api/auth";
import { getSettingsSummary } from "@/lib/api/settings";
import { clearAccessToken, getAccessToken, getRefreshToken } from "@/lib/auth/session";
import { cn } from "@/lib/utils";

import { getNavSections } from "./nav-items";

export function AppShell({ children }: { children: ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const [mobileMenuOpen, setMobileMenuOpen] = useState(false);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(
    () => typeof window !== "undefined" && window.localStorage.getItem("oneswiss:sidebarCollapsed") === "1"
  );
  const [user, setUser] = useState<AuthUser | null>(null);
  const [isLoadingProfile, setIsLoadingProfile] = useState(false);
  const [profileError, setProfileError] = useState(false);
  const [profileRetryCount, setProfileRetryCount] = useState(0);
  const [menuFeatures, setMenuFeatures] = useState({
    techLogEnabled: true,
    errorLoggingEnabled: true,
    eventLogEnabled: true,
  });

  const isLoginPage = pathname === "/login" || pathname.startsWith("/login/");

  const toggleSidebarCollapsed = () => {
    setIsSidebarCollapsed((prev) => {
      const next = !prev;
      window.localStorage.setItem("oneswiss:sidebarCollapsed", next ? "1" : "0");
      return next;
    });
  };

  const navSections = useMemo(
    () => getNavSections({ roles: user?.roles ?? [], features: menuFeatures }),
    [menuFeatures, user?.roles]
  );

  useEffect(() => {
    if (isLoginPage) {
      return;
    }

    const token = getAccessToken();
    if (!token) {
      router.replace("/login");
      return;
    }

    let cancelled = false;

    const loadProfile = async () => {
      setIsLoadingProfile(true);
      setProfileError(false);

      try {
        const [profile, summary] = await Promise.all([getCurrentUser(), getSettingsSummary()]);

        if (cancelled) {
          return;
        }

        setUser(profile);
        setMenuFeatures({
          techLogEnabled: summary.techLogEnabled,
          errorLoggingEnabled: summary.errorLoggingEnabled,
          eventLogEnabled: summary.eventLogEnabled,
        });
      } catch (error) {
        if (cancelled) {
          return;
        }

        // Разлогиниваем только при подтверждённом 401 - сетевой сбой, таймаут или
        // временная недоступность бэкенда не должны сбрасывать валидный токен.
        if (error instanceof ApiError && error.status === 401) {
          clearAccessToken();
          router.replace("/login");
        } else {
          setProfileError(true);
        }
      } finally {
        if (!cancelled) {
          setIsLoadingProfile(false);
        }
      }
    };

    void loadProfile();

    return () => {
      cancelled = true;
    };
  }, [isLoginPage, router, profileRetryCount]);

  useEffect(() => {
    const onUnauthorized = () => {
      clearAccessToken();
      router.replace("/login");
    };

    window.addEventListener("oneswiss:unauthorized", onUnauthorized);
    return () => window.removeEventListener("oneswiss:unauthorized", onUnauthorized);
  }, [router]);

  useEffect(() => {
    if (isLoginPage) {
      return;
    }

    const onSettingsUpdated = () => {
      void getSettingsSummary()
        .then((summary) => {
          setMenuFeatures({
            techLogEnabled: summary.techLogEnabled,
            errorLoggingEnabled: summary.errorLoggingEnabled,
            eventLogEnabled: summary.eventLogEnabled,
          });
        })
        .catch(() => {
          // Пункты меню обновятся при следующей навигации/перезагрузке
        });
    };

    window.addEventListener("oneswiss:settings-updated", onSettingsUpdated);
    return () => window.removeEventListener("oneswiss:settings-updated", onSettingsUpdated);
  }, [isLoginPage]);

  const onLogout = async () => {
    try {
      await logout(getRefreshToken());
    } catch {
      // ignore logout transport errors
    }

    clearAccessToken();
    setUser(null);
    router.replace("/login");
  };

  if (isLoginPage) {
    return <div className="min-h-screen bg-background">{children}</div>;
  }

  return (
    <div className="flex min-h-screen bg-background">
      <aside
        className={cn(
          "fixed inset-y-0 left-0 z-40 flex w-72 flex-col border-r bg-background transition-[transform,width] lg:sticky lg:top-0 lg:inset-auto lg:h-screen lg:translate-x-0",
          mobileMenuOpen ? "translate-x-0" : "-translate-x-full",
          isSidebarCollapsed ? "lg:w-16" : "lg:w-72"
        )}
      >
        <div className="flex h-14 shrink-0 items-center gap-2 border-b px-3">
          <div
            className={cn(
              "rounded-md bg-slate-900 px-3 py-1.5 text-white shadow-sm dark:bg-transparent dark:text-inherit dark:shadow-none",
              isSidebarCollapsed && "lg:hidden"
            )}
          >
            <OneSwissLogo className="text-base font-semibold leading-none sm:text-lg" />
          </div>

          <Button
            type="button"
            variant="ghost"
            size="icon"
            className={cn("hidden h-8 w-8 shrink-0 lg:inline-flex", isSidebarCollapsed ? "mx-auto" : "ml-auto")}
            onClick={toggleSidebarCollapsed}
            title={isSidebarCollapsed ? "Развернуть меню" : "Свернуть меню"}
            aria-label={isSidebarCollapsed ? "Развернуть меню" : "Свернуть меню"}
          >
            {isSidebarCollapsed ? <ChevronRight className="h-4 w-4" /> : <ChevronLeft className="h-4 w-4" />}
          </Button>
        </div>

        <nav className="flex flex-1 flex-col gap-3 overflow-y-auto p-3">
          <Suspense fallback={null}>
            <AgentsNavTree collapsed={isSidebarCollapsed} onNavigate={() => setMobileMenuOpen(false)} />
          </Suspense>

          {navSections.map((section, sectionIndex) => (
            <div key={`${section.title ?? "root"}-${sectionIndex}`} className="space-y-1">
              {section.title && !isSidebarCollapsed ? (
                <div className="px-3 pb-1 pt-2 text-xs font-semibold uppercase tracking-wide text-muted-foreground">
                  {section.title}
                </div>
              ) : null}

              {section.items.map((item) => {
                const Icon = item.icon;
                const isActive = pathname === item.href || pathname.startsWith(`${item.href}/`);

                return (
                  <Link
                    key={item.href}
                    href={item.href}
                    onClick={() => setMobileMenuOpen(false)}
                    title={item.title}
                    className={cn(
                      "inline-flex w-full items-center gap-2 rounded-md px-3 py-2 text-sm transition-colors",
                      isSidebarCollapsed && "lg:justify-center lg:px-2",
                      isActive
                        ? "bg-primary text-primary-foreground"
                        : "text-muted-foreground hover:bg-accent hover:text-accent-foreground"
                    )}
                  >
                    <Icon className="h-4 w-4 shrink-0" />
                    <span className={cn(isSidebarCollapsed && "lg:hidden")}>{item.title}</span>
                  </Link>
                );
              })}
            </div>
          ))}
        </nav>
      </aside>

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/70">
          <div className="flex h-14 items-center gap-3 px-4 lg:px-6">
            <Button
              type="button"
              variant="outline"
              size="icon"
              className="lg:hidden"
              onClick={() => setMobileMenuOpen((v) => !v)}
              aria-label="Открыть меню"
            >
              {mobileMenuOpen ? <X className="h-4 w-4" /> : <Menu className="h-4 w-4" />}
            </Button>

            <div className="ml-auto flex items-center gap-2">
              {user ? <div className="hidden text-sm text-muted-foreground sm:block">{user.displayName ?? user.userName}</div> : null}
              <ThemeToggle />
              <Button type="button" variant="outline" size="sm" onClick={() => void onLogout()}>
                <LogOut className="h-4 w-4" />
                Выход
              </Button>
            </div>
          </div>
        </header>

        <main className="min-w-0 flex-1 p-4 lg:p-6">
          <div className="w-full">
            {isLoadingProfile ? (
              <div className="rounded-md border bg-card p-4 text-sm text-muted-foreground">Загрузка профиля...</div>
            ) : profileError ? (
              <div className="flex flex-col items-start gap-3 rounded-md border bg-card p-4 text-sm">
                <span className="text-muted-foreground">
                  Не удалось загрузить профиль. Проверьте соединение с сервером и повторите попытку.
                </span>
                <Button
                  type="button"
                  variant="outline"
                  size="sm"
                  onClick={() => setProfileRetryCount((count) => count + 1)}
                >
                  Повторить
                </Button>
              </div>
            ) : (
              children
            )}

          </div>
        </main>
      </div>
    </div>
  );
}

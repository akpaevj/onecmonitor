import type { LucideIcon } from "lucide-react";
import {
  Activity,
  Archive,
  Bot,
  BookOpen,
  Bug,
  Database,
  FileCode2,
  FolderGit2,
  KeyRound,
  LayoutGrid,
  ScrollText,
  Settings,
  Wrench,
} from "lucide-react";

import { Roles } from "@/lib/auth/roles";

export type NavItem = {
  title: string;
  href: string;
  icon: LucideIcon;
};

export type NavSection = {
  title?: string;
  icon?: LucideIcon;
  items: NavItem[];
};

export type NavFeatures = {
  techLogEnabled: boolean;
  errorLoggingEnabled: boolean;
  eventLogEnabled: boolean;
};

export type NavContext = {
  roles: string[];
  features: NavFeatures;
};

function isAdmin(ctx: NavContext) {
  return ctx.roles.includes(Roles.Administrator);
}

function isInRoles(ctx: NavContext, ...roles: string[]) {
  return isAdmin(ctx) || roles.some((role) => ctx.roles.includes(role));
}

export function getNavSections(ctx: NavContext): NavSection[] {
  const sections: NavSection[] = [];

  // Сервисы - разделы, которыми пользуются оперативно (запуск задач, просмотр журналов)
  const serviceItems: NavItem[] = [];

  if (isInRoles(ctx, Roles.ReadMaintenanceTasks, Roles.WriteMaintenanceTasks)) {
    serviceItems.push({ title: "Задачи обслуживания", href: "/maintenancetasks", icon: Wrench });
  }

  if (isInRoles(ctx, Roles.ReadTechLogSeances, Roles.WriteTechLogSeances) && ctx.features.techLogEnabled) {
    serviceItems.push({ title: "Сеансы сбора техжурнала", href: "/techlog/seances", icon: Activity });
  }

  if (isInRoles(ctx, Roles.ReadEventLog) && ctx.features.eventLogEnabled) {
    serviceItems.push({ title: "Журнал регистрации", href: "/eventlog", icon: ScrollText });
  }

  if (isInRoles(ctx, Roles.ReadErrorLoggingReports) && ctx.features.errorLoggingEnabled) {
    serviceItems.push({ title: "Сервис регистрации ошибок", href: "/errorloggingservice", icon: Bug });
  }

  if (serviceItems.length > 0) {
    sections.push({ title: "Сервисы", icon: LayoutGrid, items: serviceItems });
  }

  // Справочники - реестры сущностей, на которые ссылаются сервисы выше (репозитории, учетки, БД)
  const referenceItems: NavItem[] = [];

  if (isInRoles(ctx, Roles.ReadMaintenanceTasks, Roles.WriteMaintenanceTasks)) {
    referenceItems.push({ title: "Хранилища конфигураций", href: "/configurationrepositories", icon: Archive });
    referenceItems.push({ title: "Конфигурации обработки и скрипты", href: "/files", icon: FileCode2 });
  }

  if (isInRoles(ctx, Roles.ReadGitRepositories, Roles.WriteGitRepositories)) {
    referenceItems.push({ title: "Репозитории Git", href: "/gitrepositories", icon: FolderGit2 });
  }

  if (isAdmin(ctx)) {
    referenceItems.push({ title: "Учетные данные и токены", href: "/credentials", icon: KeyRound });
    referenceItems.push({ title: "Секреты агентов", href: "/agentclients", icon: Bot });
    referenceItems.push({ title: "СУБД", href: "/dbms", icon: Database });
  }

  if (referenceItems.length > 0) {
    sections.push({ title: "Справочники", icon: BookOpen, items: referenceItems });
  }

  // Настройки - конфигурация приложения (доступна только администраторам)
  if (isAdmin(ctx)) {
    sections.push({
      title: "Настройки",
      icon: Settings,
      items: [{ title: "Настройки", href: "/settings", icon: Settings }],
    });
  }

  return sections;
}

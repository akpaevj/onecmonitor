import type { ReactNode } from "react";
import Link from "next/link";
import { AlertTriangle, ArrowLeft } from "lucide-react";
import type { LucideIcon } from "lucide-react";

import { Button } from "@/components/ui/button";
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from "@/components/ui/card";

export const formFieldClassName = "space-y-1.5";
export const formLabelClassName = "text-sm font-medium";
export const formControlClassName = "w-full rounded-md border bg-background px-3 py-2 text-sm";
export const formMultiSelectClassName = "min-h-28 w-full rounded-md border bg-background px-3 py-2 text-sm";
export const formCheckboxClassName = "h-4 w-4 rounded border-border";
export const formCheckboxRowClassName = "flex items-center gap-2 text-sm";
export const formActionsClassName = "flex flex-wrap gap-2 pt-1";

export function CrudFormCard({
  title,
  description,
  backHref,
  backLabel = "Назад",
  icon: Icon,
  children,
}: {
  title: string;
  description: string;
  backHref: string;
  backLabel?: string;
  icon?: LucideIcon;
  children: ReactNode;
}) {
  return (
    <Card>
      <CardHeader>
        <div className="flex items-start justify-between gap-2">
          <div>
            <CardTitle className="flex items-center gap-2">
              {Icon ? <Icon className="h-5 w-5" /> : null}
              {title}
            </CardTitle>
            <CardDescription>{description}</CardDescription>
          </div>
          <Button asChild variant="outline" size="sm">
            <Link href={backHref}>
              <ArrowLeft className="h-4 w-4" />
              {backLabel}
            </Link>
          </Button>
        </div>
      </CardHeader>
      <CardContent>{children}</CardContent>
    </Card>
  );
}

export function CrudFormLoadingCard({ title }: { title: string }) {
  return (
    <Card>
      <CardHeader>
        <CardTitle>{title}</CardTitle>
        <CardDescription>Загрузка...</CardDescription>
      </CardHeader>
    </Card>
  );
}

export function CrudFormError({ error }: { error: string | null }) {
  if (!error) return null;

  return (
    <div className="flex items-center gap-2 rounded-md border border-destructive/40 bg-destructive/10 px-3 py-2 text-sm text-destructive">
      <AlertTriangle className="h-4 w-4" />
      {error}
    </div>
  );
}

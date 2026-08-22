"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Save, ShieldCheck } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CrudFormCard,
  CrudFormError,
  CrudFormLoadingCard,
  formActionsClassName,
  formCheckboxClassName,
  formCheckboxRowClassName,
  formControlClassName,
  formFieldClassName,
  formLabelClassName,
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import {
  getAccessGroup,
  getAccessGroupRoles,
  type AccessGroupRoleItem,
  type UpsertAccessGroupRequest,
  updateAccessGroup,
} from "@/lib/api/access-groups";

type PageProps = { params: Promise<{ id: string }> };

const initialForm: UpsertAccessGroupRequest = { name: "", roleIds: [] };

export default function AccessGroupEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [roles, setRoles] = useState<AccessGroupRoleItem[]>([]);
  const [form, setForm] = useState<UpsertAccessGroupRequest>(initialForm);
  const [isBuiltIn, setIsBuiltIn] = useState(false);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void params.then((value) => setId(value.id));
  }, [params]);

  const loadData = useCallback(async () => {
    if (!id) return;
    setIsLoading(true);
    setError(null);
    try {
      const [item, rolesData] = await Promise.all([getAccessGroup(id), getAccessGroupRoles()]);
      setRoles(rolesData);
      setIsBuiltIn(item.isBuiltIn);
      setForm({ name: item.name, roleIds: [...item.roleIds] });
    } catch {
      setError("Не удалось загрузить группу доступа");
    } finally {
      setIsLoading(false);
    }
  }, [id]);

  useEffect(() => {
    const timeoutId = setTimeout(() => {
      void loadData();
    }, 0);

    return () => clearTimeout(timeoutId);
  }, [loadData]);

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    if (!id || isBuiltIn) return;
    setIsSaving(true);
    setError(null);
    setMessage(null);
    try {
      await updateAccessGroup(id, form);
      router.push("/accessgroups");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить группу доступа");
    } finally {
      setIsSaving(false);
    }
  };

  const toggleRole = (roleId: string, checked: boolean) => {
    setForm((prev) => ({
      ...prev,
      roleIds: checked ? Array.from(new Set([...prev.roleIds, roleId])) : prev.roleIds.filter((idValue) => idValue !== roleId),
    }));
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Группа доступа" />;
  }

  return (
    <CrudFormCard
      title="Редактирование группы доступа"
      description={form.name || id || "—"}
      icon={ShieldCheck}
      backHref="/accessgroups"
    >
      <form className="space-y-3" onSubmit={(event) => void onSubmit(event)}>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Наименование</label>
          <input
            className={formControlClassName}
            value={form.name}
            onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
            required
            disabled={isBuiltIn}
          />
        </div>
        <div className="space-y-2">
          <div className={formLabelClassName}>Роли</div>
          <div className="max-h-72 space-y-2 overflow-auto rounded-md border p-2">
            {roles.map((role) => (
              <label key={role.id} className={formCheckboxRowClassName}>
                <input
                  className={formCheckboxClassName}
                  type="checkbox"
                  checked={form.roleIds.includes(role.id)}
                  onChange={(event) => toggleRole(role.id, event.target.checked)}
                  disabled={isBuiltIn}
                />
                <span>{role.description || role.name}</span>
              </label>
            ))}
          </div>
        </div>
        {isBuiltIn ? <div className="text-sm text-muted-foreground">Системную группу редактировать нельзя</div> : null}
        <CrudFormError error={error} />
        {message ? <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div> : null}
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving || isBuiltIn}>
            <Save className="h-4 w-4" />Сохранить
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}

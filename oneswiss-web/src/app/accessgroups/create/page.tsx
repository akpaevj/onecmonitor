"use client";

import { useRouter } from "next/navigation";
import { Plus, ShieldCheck } from "lucide-react";
import { useEffect, useState } from "react";

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
  createAccessGroup,
  getAccessGroupRoles,
  type AccessGroupRoleItem,
  type UpsertAccessGroupRequest,
} from "@/lib/api/access-groups";

const initialForm: UpsertAccessGroupRequest = {
  name: "",
  roleIds: [],
};

export default function AccessGroupCreatePage() {
  const router = useRouter();
  const [roles, setRoles] = useState<AccessGroupRoleItem[]>([]);
  const [form, setForm] = useState<UpsertAccessGroupRequest>(initialForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const run = async () => {
      setIsLoading(true);
      setError(null);
      try {
        const data = await getAccessGroupRoles();
        if (!cancelled) {
          setRoles(data);
        }
      } catch {
        if (!cancelled) {
          setError("Не удалось загрузить роли");
        }
      } finally {
        if (!cancelled) {
          setIsLoading(false);
        }
      }
    };

    void run();

    return () => {
      cancelled = true;
    };
  }, []);

  const toggleRole = (roleId: string, checked: boolean) => {
    setForm((prev) => ({
      ...prev,
      roleIds: checked ? Array.from(new Set([...prev.roleIds, roleId])) : prev.roleIds.filter((id) => id !== roleId),
    }));
  };

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    try {
      await createAccessGroup(form);
      router.push("/accessgroups");
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать группу доступа");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Новая группа доступа" />;
  }

  return (
    <CrudFormCard
      title="Создание группы доступа"
      description="Новая группа доступа"
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
          />
        </div>
        <div className="space-y-2">
          <div className={formLabelClassName}>Роли</div>
          <div className="max-h-56 space-y-2 overflow-auto rounded-md border p-2">
            {roles.map((role) => (
              <label key={role.id} className={formCheckboxRowClassName}>
                <input
                  className={formCheckboxClassName}
                  type="checkbox"
                  checked={form.roleIds.includes(role.id)}
                  onChange={(event) => toggleRole(role.id, event.target.checked)}
                />
                <span>{role.description || role.name}</span>
              </label>
            ))}
          </div>
        </div>
        <CrudFormError error={error} />
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving}>
            <Plus className="h-4 w-4" />
            Создать
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}

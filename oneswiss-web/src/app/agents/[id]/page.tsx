"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { Save, Server } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CrudFormCard,
  CrudFormError,
  CrudFormLoadingCard,
  formActionsClassName,
  formControlClassName,
  formFieldClassName,
  formLabelClassName,
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import { getAgent, type UpsertAgentRequest, updateAgent } from "@/lib/api/agents";

type PageProps = { params: Promise<{ id: string }> };

const initialForm: UpsertAgentRequest = {
  instanceName: "",
};

export default function AgentEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [form, setForm] = useState<UpsertAgentRequest>(initialForm);
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
      const item = await getAgent(id);
      setForm({ instanceName: item.instanceName });
    } catch {
      setError("Не удалось загрузить агента");
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
    if (!id) return;

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      await updateAgent(id, form);
      router.push("/agents");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить агента");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Агент" />;
  }

  return (
    <CrudFormCard
      title="Редактирование агента"
      description={form.instanceName || id || "—"}
      icon={Server}
      backHref="/agents"
    >
      <form className="space-y-3" onSubmit={(event) => void onSubmit(event)}>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Имя инстанса</label>
          <input
            className={formControlClassName}
            value={form.instanceName}
            onChange={(event) => setForm((prev) => ({ ...prev, instanceName: event.target.value }))}
            required
          />
        </div>
        <CrudFormError error={error} />
        {message ? <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div> : null}
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving}>
            <Save className="h-4 w-4" />Сохранить
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}

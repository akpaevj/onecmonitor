"use client";

import { useRouter } from "next/navigation";
import { Database, Plus } from "lucide-react";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CrudFormCard,
  CrudFormError,
  formActionsClassName,
  formControlClassName,
  formFieldClassName,
  formLabelClassName,
} from "@/components/forms/crud-form";
import { ApiError } from "@/lib/api/client";
import { createDbms, type DbmsType, type UpsertDbmsRequest } from "@/lib/api/dbms";

const initialForm: UpsertDbmsRequest = {
  name: "",
  type: "ClickHouse",
  host: "",
  port: 8123,
};

export default function DbmsCreatePage() {
  const router = useRouter();
  const [form, setForm] = useState<UpsertDbmsRequest>(initialForm);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onTypeChanged = (type: DbmsType) => {
    setForm((prev) => ({ ...prev, type, port: 8123 }));
  };

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    try {
      await createDbms(form);
      router.push("/dbms");
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать СУБД");
      }
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <CrudFormCard title="Создание СУБД" description="Новая СУБД" icon={Database} backHref="/dbms">
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
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Тип</label>
          <select
            className={formControlClassName}
            value={form.type}
            onChange={(event) => onTypeChanged(event.target.value as DbmsType)}
          >
            <option value="ClickHouse">ClickHouse</option>
          </select>
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Хост</label>
          <input
            className={formControlClassName}
            value={form.host}
            onChange={(event) => setForm((prev) => ({ ...prev, host: event.target.value }))}
            required
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Порт</label>
          <input
            className={formControlClassName}
            type="number"
            min={1}
            value={form.port}
            onChange={(event) => setForm((prev) => ({ ...prev, port: Number(event.target.value) || 0 }))}
            required
          />
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

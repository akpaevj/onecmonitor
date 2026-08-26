"use client";

import { FileCode2, Paperclip, Plus, X } from "lucide-react";
import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { useRef, useState } from "react";

import { Button } from "@/components/ui/button";
import {
  CrudFormCard,
  CrudFormError,
  formActionsClassName,
  formControlClassName,
  formFieldClassName,
  formLabelClassName,
} from "@/components/forms/crud-form";
import { createFile } from "@/lib/api/files";

type CreateFormState = {
  name: string;
  version: string;
  file: File | null;
};

const initialForm: CreateFormState = {
  name: "",
  version: "",
  file: null,
};

export default function CreateFilePage() {
  const router = useRouter();
  const fileInputRef = useRef<HTMLInputElement | null>(null);

  const [form, setForm] = useState<CreateFormState>(initialForm);
  const [isSaving, setIsSaving] = useState(false);
  const [uploadProgress, setUploadProgress] = useState(0);
  const [error, setError] = useState<string | null>(null);

  const clearSelectedFile = () => {
    setForm((prev) => ({ ...prev, file: null }));
    if (fileInputRef.current) {
      fileInputRef.current.value = "";
    }
  };

  const onSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();

    if (!form.file) {
      setError("Прикрепите файл");
      return;
    }

    setIsSaving(true);
    setUploadProgress(0);
    setError(null);

    try {
      await createFile(
        {
          name: form.name,
          version: form.version,
          file: form.file,
        },
        {
          onUploadProgress: (progress) => setUploadProgress(progress),
        }
      );

      router.push("/files");
      router.refresh();
    } catch (e) {
      if (e instanceof Error) {
        setError(e.message);
      } else {
        setError("Не удалось создать файл");
      }
    } finally {
      setIsSaving(false);
    }
  };

  return (
    <CrudFormCard
      title="Создание файла"
      description="Файл конфигурации, обработки или скрипта"
      icon={FileCode2}
      backHref="/files"
    >
      <form className="space-y-3" onSubmit={(event) => void onSubmit(event)}>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Наименование</label>
          <input
            className={formControlClassName}
            value={form.name}
            onChange={(event) => setForm((prev) => ({ ...prev, name: event.target.value }))}
          />
        </div>

        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Версия</label>
          <input
            className={formControlClassName}
            value={form.version}
            onChange={(event) => setForm((prev) => ({ ...prev, version: event.target.value }))}
            required
          />
        </div>

        <div className="space-y-2">
          <label className={formLabelClassName}>Файл</label>
          <input
            ref={fileInputRef}
            className="hidden"
            type="file"
            accept=".cf,.cfe,.cfu,.epf,.ospx"
            onChange={(event) =>
              setForm((prev) => ({
                ...prev,
                file: event.target.files?.[0] ?? null,
              }))
            }
          />

          <div className="flex flex-wrap items-center gap-2">
            <Button type="button" variant="outline" onClick={() => fileInputRef.current?.click()}>
              <Paperclip className="h-4 w-4" />
              Прикрепить файл
            </Button>

            {form.file ? (
              <>
                <span className="rounded-md border bg-muted px-2 py-1 text-xs text-muted-foreground">
                  {form.file.name}
                </span>
                <Button type="button" variant="ghost" size="sm" onClick={clearSelectedFile}>
                  <X className="h-4 w-4" />
                  Очистить
                </Button>
              </>
            ) : (
              <span className="text-xs text-muted-foreground">Файл не выбран</span>
            )}
          </div>
        </div>

        {isSaving ? (
          <div className="space-y-1">
            <div className="flex items-center justify-between text-xs text-muted-foreground">
              <span>Загрузка файла...</span>
              <span>{uploadProgress}%</span>
            </div>
            <div className="h-2 w-full overflow-hidden rounded bg-muted">
              <div className="h-full bg-primary transition-[width] duration-150" style={{ width: `${uploadProgress}%` }} />
            </div>
          </div>
        ) : null}

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

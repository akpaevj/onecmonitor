"use client";

import type { FormEvent } from "react";
import { useRouter } from "next/navigation";
import { FolderGit2, Save } from "lucide-react";
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
import {
  getGitRepository,
  getGitTokens,
  type GitTokenItem,
  type UpsertGitRepositoryRequest,
  updateGitRepository,
} from "@/lib/api/git-repositories";

type PageProps = { params: Promise<{ id: string }> };

const initialForm: UpsertGitRepositoryRequest = {
  name: "",
  address: "",
  tokenId: "",
};

export default function GitRepositoryEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [tokens, setTokens] = useState<GitTokenItem[]>([]);
  const [form, setForm] = useState<UpsertGitRepositoryRequest>(initialForm);
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
      const [item, tokensData] = await Promise.all([getGitRepository(id), getGitTokens()]);
      setTokens(tokensData);
      setForm({ name: item.name, address: item.address, tokenId: item.tokenId || tokensData[0]?.id || "" });
    } catch {
      setError("Не удалось загрузить репозиторий");
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
      await updateGitRepository(id, form);
      router.push("/gitrepositories");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") setError(e.details);
      else setError("Не удалось сохранить репозиторий");
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Git репозиторий" />;
  }

  return (
    <CrudFormCard
      title="Редактирование Git репозитория"
      description={form.name || id || "—"}
      icon={FolderGit2}
      backHref="/gitrepositories"
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
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Адрес</label>
          <input
            className={formControlClassName}
            value={form.address}
            onChange={(event) => setForm((prev) => ({ ...prev, address: event.target.value }))}
            required
          />
        </div>
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Токен</label>
          <select
            className={formControlClassName}
            value={form.tokenId}
            onChange={(event) => setForm((prev) => ({ ...prev, tokenId: event.target.value }))}
            required
          >
            {tokens.map((token) => (
              <option key={token.id} value={token.id}>
                {token.name}
              </option>
            ))}
          </select>
        </div>
        <CrudFormError error={error} />
        {message ? <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div> : null}
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving || tokens.length === 0}>
            <Save className="h-4 w-4" />Сохранить
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}

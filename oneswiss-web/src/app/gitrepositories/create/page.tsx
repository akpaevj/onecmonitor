"use client";

import { useRouter } from "next/navigation";
import { FolderGit2, Plus } from "lucide-react";
import { useEffect, useState } from "react";

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
  createGitRepository,
  getGitTokens,
  type GitTokenItem,
  type UpsertGitRepositoryRequest,
} from "@/lib/api/git-repositories";

const initialForm: UpsertGitRepositoryRequest = {
  name: "",
  address: "",
  tokenId: "",
};

export default function GitRepositoryCreatePage() {
  const router = useRouter();
  const [tokens, setTokens] = useState<GitTokenItem[]>([]);
  const [form, setForm] = useState<UpsertGitRepositoryRequest>(initialForm);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const run = async () => {
      setIsLoading(true);
      setError(null);

      try {
        const tokensData = await getGitTokens();
        if (!cancelled) {
          setTokens(tokensData);
          setForm((prev) => ({ ...prev, tokenId: prev.tokenId || tokensData[0]?.id || "" }));
        }
      } catch {
        if (!cancelled) {
          setError("Не удалось загрузить токены");
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

  const onSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    setIsSaving(true);
    setError(null);

    try {
      await createGitRepository(form);
      router.push("/gitrepositories");
      router.refresh();
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось создать репозиторий");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Новый репозиторий GIT" />;
  }

  return (
    <CrudFormCard
      title="Создание Git репозитория"
      description="Новый репозиторий GIT"
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
        <div className={formActionsClassName}>
          <Button type="submit" disabled={isSaving || tokens.length === 0}>
            <Plus className="h-4 w-4" />
            Создать
          </Button>
        </div>
      </form>
    </CrudFormCard>
  );
}

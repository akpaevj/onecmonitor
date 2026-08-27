"use client";

import { useRouter } from "next/navigation";
import { AlertTriangle, Database, Save } from "lucide-react";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { Card, CardHeader, CardTitle } from "@/components/ui/card";
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
  getConfigurationRepository,
  getConfigurationRepositoryCredentials,
  updateConfigurationRepository,
  type ConfigurationRepositoryDetails,
  type RepositoryCredentialsItem,
} from "@/lib/api/configuration-repositories";

type PageProps = { params: Promise<{ id: string }> };

export default function ConfigurationRepositoryEditPage({ params }: PageProps) {
  const router = useRouter();
  const [id, setId] = useState<string | null>(null);
  const [credentials, setCredentials] = useState<RepositoryCredentialsItem[]>([]);
  const [details, setDetails] = useState<ConfigurationRepositoryDetails | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [isSaving, setIsSaving] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [message, setMessage] = useState<string | null>(null);

  useEffect(() => {
    void params.then((value) => setId(value.id));
  }, [params]);

  const loadData = useCallback(async () => {
    if (!id) {
      return;
    }

    setIsLoading(true);
    setError(null);

    try {
      const [repo, creds] = await Promise.all([
        getConfigurationRepository(id),
        getConfigurationRepositoryCredentials(),
      ]);
      setDetails(repo);
      setCredentials(creds);
    } catch {
      setError("Не удалось загрузить детали хранилища");
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

  const onChangeCredentials = (credentialsId: string) => {
    if (!details) {
      return;
    }

    setDetails({
      ...details,
      credentialsId,
    });
  };

  const onSave = async () => {
    if (!id || !details) {
      return;
    }

    setIsSaving(true);
    setError(null);
    setMessage(null);

    try {
      await updateConfigurationRepository(id, {
        credentialsId: details.credentialsId,
      });

      router.push("/configurationrepositories");
      router.refresh();
      return;
    } catch (e) {
      if (e instanceof ApiError && typeof e.details === "string") {
        setError(e.details);
      } else {
        setError("Не удалось сохранить изменения");
      }
    } finally {
      setIsSaving(false);
    }
  };

  if (isLoading) {
    return <CrudFormLoadingCard title="Хранилище конфигураций" />;
  }

  if (!details) {
    return (
      <Card>
        <CardHeader>
          <CardTitle className="flex items-center gap-2 text-destructive">
            <AlertTriangle className="h-5 w-5" />
            {error ?? "Хранилище не найдено"}
          </CardTitle>
        </CardHeader>
      </Card>
    );
  }

  return (
    <CrudFormCard
      title="Редактирование хранилища конфигурации"
      description={`${details.host}:${details.port}/${details.name}`}
      icon={Database}
      backHref="/configurationrepositories"
    >
      <div className="space-y-3">
        <div className={formFieldClassName}>
          <label className={formLabelClassName}>Учетные данные</label>
          <select
            className={formControlClassName}
            value={details.credentialsId ?? ""}
            onChange={(event) => onChangeCredentials(event.target.value)}
          >
            <option value="" disabled>
              Выберите учетные данные
            </option>
            {credentials.map((item) => (
              <option key={item.id} value={item.id}>
                {item.name}
              </option>
            ))}
          </select>
        </div>

        <div className="space-y-2">
          <div className={formLabelClassName}>Пользователи</div>
          {details.users.map((user) => (
            <div key={user.id} className="text-sm">
              {user.name}
            </div>
          ))}
        </div>

        <CrudFormError error={error} />
        {message && <div className="text-sm text-emerald-600 dark:text-emerald-500">{message}</div>}

        <div className={formActionsClassName}>
          <Button type="button" disabled={isSaving} onClick={() => void onSave()}>
            <Save className="h-4 w-4" />
            Сохранить
          </Button>
        </div>
      </div>
    </CrudFormCard>
  );
}

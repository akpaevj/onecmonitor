import { apiDelete, apiGet, apiPut } from "@/lib/api/client";
import { getAccessToken } from "@/lib/auth/session";
import { getApiBaseUrl } from "@/lib/runtime-config";

export type FileListItem = {
  id: string;
  name: string;
  version: string;
  fileType: string;
  fileTypeDisplay: string;
  dataPath: string;
};

export type CreateFileRequest = {
  name: string;
  version: string;
  file: File;
};

export type CreateFileOptions = {
  onUploadProgress?: (progress: number) => void;
};

export type UpdateFileRequest = {
  name: string;
  version: string;
};

export function getFiles() {
  return apiGet<FileListItem[]>("/api/files");
}

export function getFile(id: string) {
  return apiGet<FileListItem>(`/api/files/${id}`);
}

export function createFile(request: CreateFileRequest, options?: CreateFileOptions): Promise<FileListItem> {
  const formData = new FormData();
  formData.append("name", request.name);
  formData.append("version", request.version);
  formData.append("file", request.file);

  const accessToken = getAccessToken();

  return new Promise<FileListItem>((resolve, reject) => {
    const xhr = new XMLHttpRequest();

    xhr.open("POST", `${getApiBaseUrl()}/api/files`);
    xhr.setRequestHeader("Accept", "application/json");

    if (accessToken) {
      xhr.setRequestHeader("Authorization", `Bearer ${accessToken}`);
    }

    xhr.upload.onprogress = (event) => {
      if (!event.lengthComputable || !options?.onUploadProgress) {
        return;
      }

      const progress = Math.min(100, Math.round((event.loaded / event.total) * 100));
      options.onUploadProgress(progress);
    };

    xhr.onerror = () => {
      reject(new Error("Не удалось создать файл"));
    };

    xhr.onload = () => {
      const responseText = xhr.responseText || "";

      if (xhr.status < 200 || xhr.status >= 300) {
        reject(new Error(responseText || "Не удалось создать файл"));
        return;
      }

      try {
        const parsed = JSON.parse(responseText) as FileListItem;
        options?.onUploadProgress?.(100);
        resolve(parsed);
      } catch {
        reject(new Error("Некорректный ответ сервера"));
      }
    };

    xhr.send(formData);
  });
}

export function updateFile(id: string, request: UpdateFileRequest) {
  return apiPut<FileListItem, UpdateFileRequest>(`/api/files/${id}`, request);
}

export function deleteFile(id: string) {
  return apiDelete(`/api/files/${id}`);
}

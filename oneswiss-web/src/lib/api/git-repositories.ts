import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";

export type GitRepositoryListItem = {
  id: string;
  name: string;
  address: string;
  tokenId: string;
};

export type GitRepositoriesResponse = {
  items: GitRepositoryListItem[];
};

export type GitTokenItem = {
  id: string;
  name: string;
};

export type UpsertGitRepositoryRequest = {
  name: string;
  address: string;
  tokenId: string;
};

export function getGitRepositories() {
  return apiGet<GitRepositoriesResponse>("/api/gitrepositories");
}

export function getGitRepository(id: string) {
  return apiGet<GitRepositoryListItem>(`/api/gitrepositories/${id}`);
}

export function getGitTokens() {
  return apiGet<GitTokenItem[]>("/api/gitrepositories/tokens");
}

export function createGitRepository(request: UpsertGitRepositoryRequest) {
  return apiPost<GitRepositoryListItem, UpsertGitRepositoryRequest>("/api/gitrepositories", request);
}

export function updateGitRepository(id: string, request: UpsertGitRepositoryRequest) {
  return apiPut<GitRepositoryListItem, UpsertGitRepositoryRequest>(`/api/gitrepositories/${id}`, request);
}

export function deleteGitRepository(id: string) {
  return apiDelete(`/api/gitrepositories/${id}`);
}

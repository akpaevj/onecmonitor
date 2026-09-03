import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";

export type UsersGroupAccessGroupItem = {
  id: string;
  name: string;
};

export type UsersGroupListItem = {
  id: string;
  name: string;
  parentId: string | null;
  isBuiltIn: boolean;
  usersCount: number;
  accessGroupIds: string[];
  accessGroups: UsersGroupAccessGroupItem[];
};

export type UpsertUsersGroupRequest = {
  name: string;
  parentId: string | null;
  accessGroupIds: string[];
};

export type UserAccountListItem = {
  id: string;
  userName: string;
  displayName: string | null;
  externalName: string | null;
  groupId: string;
  groupName: string;
};

export type UpsertUserAccountRequest = {
  userName: string;
  groupId: string;
  displayName: string | null;
  externalName: string | null;
  password: string | null;
};

export function getUsersGroups() {
  return apiGet<UsersGroupListItem[]>("/api/users/groups");
}

export function getUsersGroup(id: string) {
  return apiGet<UsersGroupListItem>(`/api/users/groups/${id}`);
}

export function createUsersGroup(request: UpsertUsersGroupRequest) {
  return apiPost<UsersGroupListItem, UpsertUsersGroupRequest>("/api/users/groups", request);
}

export function updateUsersGroup(id: string, request: UpsertUsersGroupRequest) {
  return apiPut<UsersGroupListItem, UpsertUsersGroupRequest>(`/api/users/groups/${id}`, request);
}

export function deleteUsersGroup(id: string) {
  return apiDelete(`/api/users/groups/${id}`);
}

export function getUserAccounts(groupId?: string) {
  const search = new URLSearchParams();

  if (groupId && groupId.trim().length > 0) {
    search.set("groupId", groupId.trim());
  }

  const query = search.toString();
  const path = query.length > 0
    ? `/api/users/accounts?${query}`
    : "/api/users/accounts";

  return apiGet<UserAccountListItem[]>(path);
}

export function getUserAccount(id: string) {
  return apiGet<UserAccountListItem>(`/api/users/accounts/${id}`);
}

export function createUserAccount(request: UpsertUserAccountRequest) {
  return apiPost<UserAccountListItem, UpsertUserAccountRequest>("/api/users/accounts", request);
}

export function updateUserAccount(id: string, request: UpsertUserAccountRequest) {
  return apiPut<UserAccountListItem, UpsertUserAccountRequest>(`/api/users/accounts/${id}`, request);
}

export function deleteUserAccount(id: string) {
  return apiDelete(`/api/users/accounts/${id}`);
}

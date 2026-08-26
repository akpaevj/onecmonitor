import { apiDelete, apiGet, apiPost, apiPut } from "@/lib/api/client";

export type AccessGroupRoleItem = {
  id: string;
  name: string;
  description: string;
};

export type AccessGroupListItem = {
  id: string;
  name: string;
  isBuiltIn: boolean;
  roleIds: string[];
  roles: AccessGroupRoleItem[];
  usersGroupsCount: number;
};

export type UpsertAccessGroupRequest = {
  name: string;
  roleIds: string[];
};

export function getAccessGroups() {
  return apiGet<AccessGroupListItem[]>("/api/accessgroups");
}

export function getAccessGroup(id: string) {
  return apiGet<AccessGroupListItem>(`/api/accessgroups/${id}`);
}

export function getAccessGroupRoles() {
  return apiGet<AccessGroupRoleItem[]>("/api/accessgroups/roles");
}

export function createAccessGroup(request: UpsertAccessGroupRequest) {
  return apiPost<AccessGroupListItem, UpsertAccessGroupRequest>("/api/accessgroups", request);
}

export function updateAccessGroup(id: string, request: UpsertAccessGroupRequest) {
  return apiPut<AccessGroupListItem, UpsertAccessGroupRequest>(`/api/accessgroups/${id}`, request);
}

export function deleteAccessGroup(id: string) {
  return apiDelete(`/api/accessgroups/${id}`);
}

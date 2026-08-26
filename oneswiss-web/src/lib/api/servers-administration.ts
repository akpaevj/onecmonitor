import { apiGet, apiPost, apiPut } from "@/lib/api/client";

export type InfoBaseOverviewItem = {
  id: string;
  name: string;
  infoBaseName: string;
  infoBaseInternalId: string;
  publishAddress: string;
};

export type ClusterOverviewItem = {
  id: string;
  name: string;
  host: string;
  port: number;
  ragentPort: number;
  clusterInternalId: string;
  infoBases: InfoBaseOverviewItem[];
};

export type ServerAgentOverviewItem = {
  id: string;
  instanceName: string;
  isConnected: boolean;
  canOpenSessions: boolean;
  clusters: ClusterOverviewItem[];
};

export type V8SessionItem = {
  id: string;
  sessionId: string;
  infoBaseName: string;
  infoBaseId: string;
  connectionId: string;
  processPid: number;
  userName: string;
  host: string;
  appId: string;
  locale: string;
  startedAt: string;
  lastActiveAt: string;
  dbProcInfo: string;
  dbProcTook: number;
  dbProcTookAt: string;
  blockedByDbms: number;
  blockedByLs: number;
  passiveSessionHibernateTime: number;
  durationCurrentDbms: number;
  durationLast5MinDbms: number;
  durationAllDbms: number;
  dbmsBytesLast5Min: number;
  dbmsBytesAll: number;
  durationCurrent: number;
  durationLast5Min: number;
  durationAll: number;
  callsLast5Min: number;
  callsAll: number;
  bytesLast5Min: number;
  bytesAll: number;
  memoryCurrent: number;
  memoryLast5Min: number;
  memoryTotal: number;
  readCurrent: number;
  readLast5Min: number;
  readTotal: number;
  writeCurrent: number;
  writeLast5Min: number;
  writeTotal: number;
  hibernate: boolean;
  hibernateSessionTerminateTime: number;
  durationCurrentService: number;
  currentServiceName: string;
  durationLast5MinService: number;
  durationAllService: number;
  cpuTimeCurrent: number;
  cpuTimeLast5Min: number;
  cpuTimeTotal: number;
  clientIp: string;
  dataSeparation: string;
};

export type V8ProcessItem = {
  id: string;
  host: string;
  port: number;
  pid: number;
  turnedOn: boolean;
  running: boolean;
  startedAt: string;
  use: boolean;
  availablePerfomance: number;
  capacity: number;
  connections: number;
  memorySize: number;
  memoryExcessTime: number;
  selectionSize: number;
  avgCallTime: number;
  avgDbCallTime: number;
  avgLockCallTime: number;
  avgServerCallTime: number;
  avgThreads: number;
  reserve: boolean;
};

export type V8LicenseItem = {
  source: string;
  processId: string;
  sessionId: string;
  host: string;
  port: number;
  pid: number;
  fullName: string;
  series: string;
  issuedByServer: boolean;
  licenseType: string;
  net: boolean;
  maxUsersAll: number;
  maxUsersCur: number;
  rmngrAddress: string;
  rmngrPort: number;
  rmngrPid: number;
  shortPresentation: string;
  fullPresentation: string;
};

export type V8ConnectionItem = {
  id: string;
  connId: number;
  host: string;
  processPid: number;
  infoBaseId: string;
  infoBaseName: string;
  application: string;
  connectedAt: string;
  sessionNumber: number;
  blockedByLs: number;
};

export type V8LockItem = {
  object: string;
  locked: string;
  descr: string;
  sessionId: string;
  sessionNumber: string;
  connectionId: string;
  connectionNumber: number;
  infoBaseId: string;
  infoBaseName: string;
  userName: string;
  host: string;
};

export type V8ServerItem = {
  id: string;
  agentHost: string;
  agentPort: number;
  portRange: string;
  name: string;
  using: string;
  dedicateManagers: string;
  infoBasesLimit: number;
  memoryLimit: number;
  connectionsLimit: number;
  safeWorkingProcessesMemoryLimit: number;
  safeCallMemoryLimit: number;
  clusterPort: number;
  criticalTotalMemory: number;
  temporaryAllowedTotalMemory: number;
  temporaryAllowedTotalMemoryTimeLimit: number;
  servicePrincipalName: string;
  restartSchedule: string;
};

export type V8ManagerItem = {
  id: string;
  pid: number;
  using: string;
  host: string;
  port: number;
  descr: string;
};

export type V8ManagerServiceItem = {
  name: string;
  mainOnly: boolean;
  managerId: string;
  descr: string;
};

export type V8AgentVersionItem = {
  version: string;
};

export type V8SecurityProfileItem = {
  name: string;
  descr: string;
  config: boolean;
  priv: boolean;
  fullPrivilegedMode: boolean;
  privilegedModeRoles: string;
  crypto: boolean;
  rightExtension: boolean;
  rightExtensionDefinitionRoles: string;
  allModulesExtension: boolean;
  modulesAvailableForExtension: string;
  modulesNotAvailableForExtension: string;
};

export type V8ResourceCounterItem = {
  name: string;
  descr: string;
  collectionTime: string;
  group: string;
  filterType: string;
  filter: string;
  duration: string;
  cpuTime: string;
  memory: string;
  read: string;
  write: string;
  durationDbms: string;
  dbmsBytes: string;
  service: string;
  call: string;
  numberOfActiveSessions: string;
  numberOfSessions: string;
};

export type V8ResourceLimitItem = {
  name: string;
  descr: string;
  action: string;
  counter: string;
  duration: number;
  cpuTime: number;
  memory: number;
  read: number;
  write: number;
  durationDbms: number;
  dbmsBytes: number;
  service: number;
  call: number;
  numberOfActiveSessions: number;
  numberOfSessions: number;
  errorMessage: string;
};

export type V8AssignmentRuleItem = {
  id: string;
  position: number;
  objectType: string;
  infoBaseName: string;
  ruleType: string;
  applicationExt: string;
  priority: number;
};

export type V8ServiceSettingItem = {
  id: string;
  serviceName: string;
  infoBaseName: string;
  serviceDataDir: string;
};

export type V8BinaryDataStorageItem = {
  id: string;
  name: string;
};

export type LookupItem = {
  id: string;
  name: string;
};

export type AgentDetailsItem = {
  id: string;
  instanceName: string;
  isConnected: boolean;
  agentVersion: string | null;
  hostName: string | null;
  installedPlatforms: V8PlatformItem[];
  ragentServices: RagentServiceItem[];
  rasServices: RasServiceItem[];
  crServerServices: CrServerServiceItem[];
  edtInstallations: EdtInstallationItem[];
};

export type V8PlatformItem = {
  version: string;
};

export type RagentServiceItem = {
  name: string;
  isActive: boolean;
  port: number;
  debugType: string;
};

export type RasServiceItem = {
  name: string;
  isActive: boolean;
  ragentHost: string;
  ragentPort: number;
  port: number;
};

export type CrServerServiceItem = {
  name: string;
  isActive: boolean;
  port: number;
};

export type EdtInstallationItem = {
  version: string;
  fromStarter: boolean;
};

export type ClusterOneSwissItem = {
  id: string;
  name: string;
  port: number;
  credentialsId: string | null;
};

export type ClusterOneSwissResponse = {
  item: ClusterOneSwissItem;
  credentials: LookupItem[];
};

export type InfoBaseOneSwissItem = {
  id: string;
  name: string;
  infoBaseName: string;
  credentialsId: string | null;
};

export type InfoBaseOneSwissResponse = {
  item: InfoBaseOneSwissItem;
  credentials: LookupItem[];
};

export type V8SecurityLevel = "Disabled" | "OnlyConnection" | "Enabled";
export type V8ClusterLoadBalancingMode = "Performance" | "Memory";
export type V8InfoBaseDbms = "MsSqlServer" | "PostgreSql" | "IbmDb2" | "OracleDatabase";

export type V8ClusterDetailsItem = {
  id: string;
  name: string;
  securityLevel: V8SecurityLevel;
  allowAccessRightAuditEventsRecording: boolean;
  restartSchedule: string;
  killProblemProcesses: boolean;
  killByMemoryWithDump: boolean;
  expirationTimeout: number;
  sessionFaultToleranceLevel: number;
  loadBalancingMode: V8ClusterLoadBalancingMode;
  pingPeriod: number;
  pingTimeout: number;
};

export type V8InfoBaseDetailsItem = {
  id: string;
  name: string;
  description: string;
  securityLevel: V8SecurityLevel;
  dbms: V8InfoBaseDbms;
  dbServer: string;
  dbName: string;
  dbUser: string;
  dbPwd: string;
  licenseDistribution: boolean;
  sessionsDeny: boolean;
  scheduledJobsDeny: boolean;
  deniedFrom: string;
  deniedTo: string;
  deniedMessage: string;
  permissionCode: string;
  deniedParameter: string;
  externalSessionManagerConnectionString: string;
  externalSessionManagerRequired: boolean;
  securityProfileName: string;
  safeModeSecurityProfileName: string;
  reserveWorkingProcesses: boolean;
  disableLocalSpeechToText: boolean;
  configurationUnloadDelayByWorkingProcessWithoutActiveUsers: number;
  minimumScheduledJobsStartPeriodWithoutActiveUsers: number;
  maximumScheduledJobsStartShiftWithoutActiveUsers: number;
};

export function getServersAdministrationOverview() {
  return apiGet<ServerAgentOverviewItem[]>("/api/serversadministration/overview");
}

export function getAgentDetails(id: string) {
  return apiGet<AgentDetailsItem>(`/api/serversadministration/agents/${id}`);
}

export function getClusterSessions(clusterId: string, infoBaseId?: string) {
  const search = new URLSearchParams();

  if (infoBaseId && infoBaseId.trim().length > 0) {
    search.set("infoBaseId", infoBaseId.trim());
  }

  const query = search.toString();
  const path = query.length > 0
    ? `/api/serversadministration/clusters/${clusterId}/sessions?${query}`
    : `/api/serversadministration/clusters/${clusterId}/sessions`;

  return apiGet<V8SessionItem[]>(path);
}

export function getClusterConnections(clusterId: string, infoBaseId?: string) {
  const search = new URLSearchParams();

  if (infoBaseId && infoBaseId.trim().length > 0) {
    search.set("infoBaseId", infoBaseId.trim());
  }

  const query = search.toString();
  const path = query.length > 0
    ? `/api/serversadministration/clusters/${clusterId}/connections?${query}`
    : `/api/serversadministration/clusters/${clusterId}/connections`;

  return apiGet<V8ConnectionItem[]>(path);
}

export function closeClusterSessions(clusterId: string, sessionIds: string[]) {
  return apiPost<void, { sessionIds: string[] }>(
    `/api/serversadministration/clusters/${clusterId}/sessions/close`,
    { sessionIds }
  );
}

export function getClusterLocks(clusterId: string, infoBaseId?: string) {
  const search = new URLSearchParams();

  if (infoBaseId && infoBaseId.trim().length > 0) {
    search.set("infoBaseId", infoBaseId.trim());
  }

  const query = search.toString();
  const path = query.length > 0
    ? `/api/serversadministration/clusters/${clusterId}/locks?${query}`
    : `/api/serversadministration/clusters/${clusterId}/locks`;

  return apiGet<V8LockItem[]>(path);
}

export function getClusterServers(clusterId: string) {
  return apiGet<V8ServerItem[]>(`/api/serversadministration/clusters/${clusterId}/servers`);
}

export function getClusterManagers(clusterId: string) {
  return apiGet<V8ManagerItem[]>(`/api/serversadministration/clusters/${clusterId}/managers`);
}

export function getClusterManagerServices(clusterId: string) {
  return apiGet<V8ManagerServiceItem[]>(`/api/serversadministration/clusters/${clusterId}/services`);
}

export function getClusterAgentVersion(clusterId: string) {
  return apiGet<V8AgentVersionItem>(`/api/serversadministration/clusters/${clusterId}/agent-version`);
}

export function getClusterSecurityProfiles(clusterId: string) {
  return apiGet<V8SecurityProfileItem[]>(`/api/serversadministration/clusters/${clusterId}/securityprofiles`);
}

export function getClusterResourceCounters(clusterId: string) {
  return apiGet<V8ResourceCounterItem[]>(`/api/serversadministration/clusters/${clusterId}/resourcecounters`);
}

export function getClusterResourceLimits(clusterId: string) {
  return apiGet<V8ResourceLimitItem[]>(`/api/serversadministration/clusters/${clusterId}/resourcelimits`);
}

export function getClusterServerRules(clusterId: string, serverId: string) {
  return apiGet<V8AssignmentRuleItem[]>(
    `/api/serversadministration/clusters/${clusterId}/servers/${serverId}/rules`
  );
}

export function getClusterServerServiceSettings(clusterId: string, serverId: string) {
  return apiGet<V8ServiceSettingItem[]>(
    `/api/serversadministration/clusters/${clusterId}/servers/${serverId}/servicesettings`
  );
}

export function getInfoBaseBinaryDataStorages(infoBaseId: string) {
  return apiGet<V8BinaryDataStorageItem[]>(`/api/serversadministration/infobases/${infoBaseId}/binarydatastorages`);
}

export function getClusterLicenses(clusterId: string) {
  return apiGet<V8LicenseItem[]>(`/api/serversadministration/clusters/${clusterId}/licenses`);
}

export function getClusterProcesses(clusterId: string) {
  return apiGet<V8ProcessItem[]>(`/api/serversadministration/clusters/${clusterId}/processes`);
}

export function getClusterProcess(clusterId: string, processId: string) {
  return apiGet<V8ProcessItem>(`/api/serversadministration/clusters/${clusterId}/processes/${processId}`);
}

export function getClusterOneSwiss(id: string) {
  return apiGet<ClusterOneSwissResponse>(`/api/serversadministration/clusters/${id}/oneswiss`);
}

export function saveClusterOneSwiss(id: string, credentialsId: string | null) {
  return apiPut<ClusterOneSwissItem, { credentialsId: string | null }>(
    `/api/serversadministration/clusters/${id}/oneswiss`,
    { credentialsId }
  );
}

export function getInfoBaseOneSwiss(id: string) {
  return apiGet<InfoBaseOneSwissResponse>(`/api/serversadministration/infobases/${id}/oneswiss`);
}

export function saveInfoBaseOneSwiss(id: string, credentialsId: string | null) {
  return apiPut<InfoBaseOneSwissItem, { credentialsId: string | null }>(
    `/api/serversadministration/infobases/${id}/oneswiss`,
    { credentialsId }
  );
}

export function getClusterV8Details(id: string) {
  return apiGet<V8ClusterDetailsItem>(`/api/serversadministration/clusters/${id}/v8`);
}

export function saveClusterV8Details(id: string, item: V8ClusterDetailsItem) {
  return apiPut<V8ClusterDetailsItem, V8ClusterDetailsItem>(`/api/serversadministration/clusters/${id}/v8`, item);
}

export function getInfoBaseV8Details(id: string) {
  return apiGet<V8InfoBaseDetailsItem>(`/api/serversadministration/infobases/${id}/v8`);
}

export function saveInfoBaseV8Details(id: string, item: V8InfoBaseDetailsItem) {
  return apiPut<V8InfoBaseDetailsItem, V8InfoBaseDetailsItem>(`/api/serversadministration/infobases/${id}/v8`, item);
}

export const Roles = {
  Administrator: "Administrator",
  ReadMaintenanceTasks: "ReadMaintenanceTasks",
  WriteMaintenanceTasks: "WriteMaintenanceTasks",
  ReadTechLogSeances: "ReadTechLogSeances",
  WriteTechLogSeances: "WriteTechLogSeances",
  ReadErrorLoggingReports: "ReadErrorLoggingReports",
  ReadEventLog: "ReadEventLog",
  ReadBuildTasks: "ReadBuildTasks",
  ConfigBuildTasks: "ConfigBuildTasks",
} as const;

export type RoleName = (typeof Roles)[keyof typeof Roles];

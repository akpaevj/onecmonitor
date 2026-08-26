"use client";

import { MaintenanceStructureEditor } from "@/components/maintenance/maintenance-structure-editor";

type MaintenanceTaskEditorPageProps = {
  params: Promise<{ id: string }>;
};

export default function MaintenanceTaskEditorPage({ params }: MaintenanceTaskEditorPageProps) {
  return <MaintenanceStructureEditor params={params} mode="task" />;
}

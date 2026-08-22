"use client";

import { MaintenanceStructureEditor } from "@/components/maintenance/maintenance-structure-editor";

type MaintenanceTaskTemplateEditorPageProps = {
  params: Promise<{ id: string }>;
};

export default function MaintenanceTaskTemplateEditorPage({ params }: MaintenanceTaskTemplateEditorPageProps) {
  return <MaintenanceStructureEditor params={params} mode="template" />;
}

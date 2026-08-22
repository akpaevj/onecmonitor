import { redirect } from "next/navigation";

export default function LegacyAccessDeniedRoutePage() {
  redirect("/login");
}

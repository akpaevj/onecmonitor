import { redirect } from "next/navigation";

export default function LegacyInvalidUserRoutePage() {
  redirect("/login");
}

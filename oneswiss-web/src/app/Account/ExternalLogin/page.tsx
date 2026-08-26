import { redirect } from "next/navigation";

export default function LegacyExternalLoginRoutePage() {
  redirect("/login");
}

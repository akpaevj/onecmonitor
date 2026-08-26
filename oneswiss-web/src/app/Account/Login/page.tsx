import { redirect } from "next/navigation";

export default function LegacyLoginRoutePage() {
  redirect("/login");
}

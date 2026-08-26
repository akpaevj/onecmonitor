import { cn } from "@/lib/utils";

type OneSwissLogoProps = {
  className?: string;
  textClassName?: string;
};

export function OneSwissLogo({ className, textClassName }: OneSwissLogoProps) {
  return (
    <div className={cn("inline-flex items-center justify-center gap-0", className)}>
      <span className={cn(textClassName)} style={{ color: "#ffe016" }}>
        One
      </span>
      <span className={cn(textClassName)}>Sw</span>
      <span className={cn(textClassName)} style={{ color: "#ffe016" }}>
        i
      </span>
      <span className={cn(textClassName)}>ss</span>
    </div>
  );
}

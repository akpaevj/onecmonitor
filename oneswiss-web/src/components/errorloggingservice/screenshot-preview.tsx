"use client";

import { Minus, Plus, Scan } from "lucide-react";
import { useState } from "react";
import { TransformComponent, TransformWrapper, useControls, useTransformComponent } from "react-zoom-pan-pinch";

import { Button } from "@/components/ui/button";
import { Dialog, DialogContent, DialogTitle } from "@/components/ui/dialog";

type ScreenshotPreviewProps = {
  src: string;
  compact?: boolean;
};

export function ScreenshotPreview({ src, compact = false }: ScreenshotPreviewProps) {
  const [open, setOpen] = useState(false);

  return (
    <>
      <button
        type="button"
        onClick={() => setOpen(true)}
        className="block w-full cursor-zoom-in overflow-hidden rounded-md border"
      >
        {/* eslint-disable-next-line @next/next/no-img-element -- скриншот приходит как data URL с backend, next/image не применим */}
        <img
          src={src}
          alt="Скриншот экрана пользователя"
          className={compact ? "max-h-40 w-full object-cover object-top" : "w-full object-contain"}
        />
      </button>

      {/* Radix полностью размонтирует содержимое при закрытии, поэтому масштаб
          из предыдущего просмотра не переиспользуется - сбрасывать его вручную не нужно. */}
      <Dialog open={open} onOpenChange={setOpen}>
        <DialogContent className="flex h-[90vh] w-[95vw] max-w-[95vw] flex-col sm:max-w-[95vw]">
          <DialogTitle>Скриншот экрана пользователя</DialogTitle>
          {open ? (
            <TransformWrapper
              initialScale={1}
              minScale={0.1}
              maxScale={8}
              centerOnInit
              fitOnInit="contain"
              // По умолчанию библиотека умножает шаг колеса на event.deltaY ("smooth" режим,
              // рассчитанный на трекпад с частыми мелкими событиями) - для обычного мышиного
              // колеса (deltaY ~100 за щелчок) это давало скачок сразу к максимуму за один щелчок.
              smooth={false}
              wheel={{ step: 0.15 }}
              doubleClick={{ mode: "toggle" }}
            >
              <ScreenshotZoomToolbar />
              <div className="min-h-0 flex-1 overflow-hidden rounded-md border bg-muted/20">
                <TransformComponent wrapperStyle={{ width: "100%", height: "100%" }}>
                  {/* eslint-disable-next-line @next/next/no-img-element -- скриншот приходит как data URL с backend, next/image не применим */}
                  <img src={src} alt="Скриншот экрана пользователя" />
                </TransformComponent>
              </div>
            </TransformWrapper>
          ) : null}
        </DialogContent>
      </Dialog>
    </>
  );
}

function ScreenshotZoomToolbar() {
  const { zoomIn, zoomOut, resetTransform } = useControls();
  const scale = useTransformComponent((context) => context.state.scale);

  return (
    <div className="flex items-center justify-center gap-1">
      <Button type="button" variant="outline" size="icon" onClick={() => zoomOut()}>
        <Minus className="h-4 w-4" />
      </Button>
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={() => resetTransform()}
        className="min-w-16 tabular-nums"
        title="По размеру окна"
      >
        {Math.round(scale * 100)}%
      </Button>
      <Button type="button" variant="outline" size="icon" onClick={() => zoomIn()}>
        <Plus className="h-4 w-4" />
      </Button>
      <Button type="button" variant="outline" size="icon" onClick={() => resetTransform()} title="По размеру окна">
        <Scan className="h-4 w-4" />
      </Button>
    </div>
  );
}

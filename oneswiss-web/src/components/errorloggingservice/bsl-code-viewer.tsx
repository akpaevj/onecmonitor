"use client";

import { Editor, loader, type BeforeMount, type Monaco } from "@monaco-editor/react";
import { useMemo } from "react";

import { useTheme } from "@/components/theme/theme-provider";

if (typeof window !== "undefined") {
  loader.config({ paths: { vs: "/monaco-editor/vs" } });
}

const BSL_LANGUAGE_ID = "bsl";

const BSL_KEYWORDS = [
  "Если", "Тогда", "ИначеЕсли", "Иначе", "КонецЕсли",
  "Для", "Каждого", "Из", "По", "Цикл", "КонецЦикла", "Пока",
  "Прервать", "Продолжить",
  "Процедура", "КонецПроцедуры", "Функция", "КонецФункции", "Возврат",
  "Перем", "Знач", "Экспорт",
  "Попытка", "Исключение", "КонецПопытки", "ВызватьИсключение",
  "Новый", "Выполнить", "Не", "И", "Или",
  "Истина", "Ложь", "Неопределено", "NULL",
];

const BSL_TYPE_KEYWORDS = ["Число", "Строка", "Дата", "Булево", "Массив", "Структура", "Соответствие"];

let bslLanguageRegistered = false;

function registerBslLanguage(monaco: Monaco) {
  if (bslLanguageRegistered) {
    return;
  }

  bslLanguageRegistered = true;
  monaco.languages.register({ id: BSL_LANGUAGE_ID });

  monaco.languages.setLanguageConfiguration(BSL_LANGUAGE_ID, {
    comments: { lineComment: "//" },
    brackets: [
      ["(", ")"],
      ["[", "]"],
    ],
  });

  monaco.languages.setMonarchTokensProvider(BSL_LANGUAGE_ID, {
    ignoreCase: true,
    keywords: BSL_KEYWORDS,
    typeKeywords: BSL_TYPE_KEYWORDS,
    operators: ["=", "<", ">", "<=", ">=", "<>", "+", "-", "*", "/", "%"],
    symbols: /[=><!~?:&|+\-*/^%]+/,
    tokenizer: {
      root: [
        [/&[A-Za-zА-Яа-яЁё]+/, "annotation"],
        [/#[A-Za-zА-Яа-яЁё]+/, "keyword.directive"],
        [
          /[A-Za-zА-Яа-яЁё_][\wА-Яа-яЁё]*/,
          {
            cases: {
              "@keywords": "keyword",
              "@typeKeywords": "type",
              "@default": "identifier",
            },
          },
        ],
        [/\d+(\.\d+)?/, "number"],
        [/"([^"]|"")*"/, "string"],
        [/\/\/.*$/, "comment"],
        [/[{}()[\]]/, "@brackets"],
        [/@symbols/, { cases: { "@operators": "operator", "@default": "" } }],
      ],
    },
  });
}

const handleBeforeMount: BeforeMount = (monaco) => {
  registerBslLanguage(monaco);
};

type BslCodeViewerProps = {
  code: string;
  compact?: boolean;
};

export function BslCodeViewer({ code, compact = false }: BslCodeViewerProps) {
  const { resolvedTheme } = useTheme();

  const height = useMemo(() => {
    const lineHeight = compact ? 18 : 20;
    const maxHeight = compact ? 220 : 360;
    const lineCount = code.split("\n").length;

    return Math.min(lineCount * lineHeight + 16, maxHeight);
  }, [code, compact]);

  return (
    <div className="overflow-hidden rounded-md border">
      <Editor
        height={height}
        language={BSL_LANGUAGE_ID}
        value={code}
        theme={resolvedTheme === "dark" ? "vs-dark" : "light"}
        beforeMount={handleBeforeMount}
        loading={<div className="p-3 text-xs text-muted-foreground">Загрузка редактора кода...</div>}
        options={{
          readOnly: true,
          domReadOnly: true,
          minimap: { enabled: false },
          lineNumbers: compact ? "off" : "on",
          fontSize: compact ? 12 : 13,
          scrollBeyondLastLine: false,
          folding: false,
          wordWrap: "on",
          renderLineHighlight: "none",
          overviewRulerLanes: 0,
          hideCursorInOverviewRuler: true,
          contextmenu: false,
          scrollbar: { alwaysConsumeMouseWheel: false },
        }}
      />
    </div>
  );
}
